using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApp1
{
    public enum TipoPedina { Nessuna, Bianca, Nera, BiancaDama, NeraDama }

    public class Cella
    {
        public int Riga { get; set; }
        public int Colonna { get; set; }
        public TipoPedina Pedina { get; set; }
    }

    public class Scacchiera
    {
        public Cella[,] Griglia { get; set; } = new Cella[8, 8];
        public TipoPedina TurnoCorrente { get; set; } = TipoPedina.Bianca;
        public Cella CellaSelezionata { get; set; } = null;
        public List<Cella> MosseValide { get; set; } = new List<Cella>();
    }

    public partial class MainWindow : Window
    {
        private Scacchiera Scacchiera = new Scacchiera();

        public MainWindow()
        {
            InizializzaScacchiera();
        }

        //inizializzazione

        private void InizializzaScacchiera()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    Scacchiera.Griglia[r, c] = new Cella { Riga = r, Colonna = c, Pedina = TipoPedina.Nessuna };

                    // Pedine nere nelle prime 3 righe
                    if (r < 3 && (r + c) % 2 == 1)
                        Scacchiera.Griglia[r, c].Pedina = TipoPedina.Nera;

                    // Pedine bianche nelle ultime 3 righe
                    if (r > 4 && (r + c) % 2 == 1)
                        Scacchiera.Griglia[r, c].Pedina = TipoPedina.Bianca;
                }
        }

        // click cella

        public void AlClickCella(int riga, int colonna)
        {
            Cella cliccata = Scacchiera.Griglia[riga, colonna];

            // Punto 3: deseleziona se clicco la stessa pedina
            if (Scacchiera.CellaSelezionata == cliccata)
            {
                Scacchiera.CellaSelezionata = null;
                Scacchiera.MosseValide.Clear();
                return;
            }

            // clicco mia pedina
            if (ÈMia(cliccata))
            {
                var mosse = GetMosseValide(cliccata);

                // Punto 4: non selezionabile se non ha mosse
                if (mosse.Count == 0) return;

                Scacchiera.CellaSelezionata = cliccata;
                Scacchiera.MosseValide = mosse;
            }
            // clicco mossa valida
            else if (Scacchiera.MosseValide.Contains(cliccata))
            {
                EseguiMossa(Scacchiera.CellaSelezionata, cliccata);
            }
        }

        // ─── MOSSE VALIDE ───────────────────────────────────────────────

        public List<Cella> GetMosseValide(Cella pedina)
        {
            // obbligo di mangiare
            if (CiSonoCatture())
            {
                return GetCatture(pedina);
            }

            return GetMosseSemplici(pedina);
        }

        public List<Cella> GetMosseSemplici(Cella pedina)
        {
            var mosse = new List<Cella>();
            bool èDama = pedina.Pedina == TipoPedina.BiancaDama || pedina.Pedina == TipoPedina.NeraDama;

            // direzione in base al colore
            int[] direzioni = èDama
                ? new[] { -1, 1 }
                : (pedina.Pedina == TipoPedina.Bianca ? new[] { -1 } : new[] { 1 });

            for (int i = 0; i < direzioni.Length; i++)
            {
                int dr = direzioni[i];
                for (int j = 0; j < 2; j++)
                {
                    int dc = (j == 0) ? -1 : 1;

                    if (èDama)
                    {
                        int r = pedina.Riga + dr;
                        int c = pedina.Colonna + dc;
                        while (ÈInBounds(r, c) &&
                               Scacchiera.Griglia[r, c].Pedina == TipoPedina.Nessuna)
                        {
                            mosse.Add(Scacchiera.Griglia[r, c]);
                            r += dr;
                            c += dc;
                        }
                    }
                    else
                    {
                        // pedina normale, solo +1
                        int nuovaRiga = pedina.Riga + dr;
                        int nuovaColonna = pedina.Colonna + dc;
                        if (ÈInBounds(nuovaRiga, nuovaColonna) &&
                            Scacchiera.Griglia[nuovaRiga, nuovaColonna].Pedina == TipoPedina.Nessuna)
                        {
                            mosse.Add(Scacchiera.Griglia[nuovaRiga, nuovaColonna]);
                        }
                    }
                }
            }
            return mosse;
        }

        // catture

        public List<Cella> GetCatture(Cella pedina)
        {
            var catture = new List<Cella>();
            int[] drs = { -1, 1 };
            int[] dcs = { -1, 1 };

            for (int i = 0; i < drs.Length; i++)
            {
                int dr = drs[i];
                for (int j = 0; j < dcs.Length; j++)
                {
                    int dc = dcs[j];

                    int rigaMedia = pedina.Riga + dr;
                    int colonnaMedia = pedina.Colonna + dc;
                    int rigaArrivo = pedina.Riga + 2 * dr;
                    int colonnaArrivo = pedina.Colonna + 2 * dc;

                    if (ÈInBounds(rigaArrivo, colonnaArrivo) &&
                        ÈNemica(Scacchiera.Griglia[rigaMedia, colonnaMedia], pedina) &&
                        Scacchiera.Griglia[rigaArrivo, colonnaArrivo].Pedina == TipoPedina.Nessuna)
                    {
                        catture.Add(Scacchiera.Griglia[rigaArrivo, colonnaArrivo]);
                    }
                }
            }

            return catture;
        }

        // ESECUZIONE MOSSA 

        public void EseguiMossa(Cella da, Cella a)
        {
            // Sposta la pedina
            a.Pedina = da.Pedina;
            da.Pedina = TipoPedina.Nessuna;

            bool èStataMangIata = Math.Abs(a.Riga - da.Riga) == 2;

            if (èStataMangIata)
            {
                RimuoviPedinaMangIata(da, a);
                ControllaPromozione(a);

                // Punto 9: presa multipla
                var prossimeCatture = GetCatture(a);
                if (prossimeCatture.Count > 0)
                {
                    Scacchiera.CellaSelezionata = a;
                    Scacchiera.MosseValide = prossimeCatture;
                    return; // non cambia turno!
                }
            }

            ControllaPromozione(a); // Punto 10
            Scacchiera.CellaSelezionata = null;
            Scacchiera.MosseValide.Clear();
            CambiaTurno();
            ControllаVittoria(); // Punti 5 e 6
        }

        //RIMUOVI PEDINA MANGIATA

        private void RimuoviPedinaMangIata(Cella da, Cella a)
        {
            int rigaMedia = (da.Riga + a.Riga) / 2;
            int colonnaMedia = (da.Colonna + a.Colonna) / 2;
            Scacchiera.Griglia[rigaMedia, colonnaMedia].Pedina = TipoPedina.Nessuna;
        }

        //PROMOZIONE A DAMA 

        public void ControllaPromozione(Cella cella)
        {
            if (cella.Pedina == TipoPedina.Bianca && cella.Riga == 0)
                cella.Pedina = TipoPedina.BiancaDama;
            if (cella.Pedina == TipoPedina.Nera && cella.Riga == 7)
                cella.Pedina = TipoPedina.NeraDama;
        }

        // CONDIZIONI DI VITTORIA 

        public void ControllаVittoria()
        {
            bool biancheEsistono = PedineEsistono(TipoPedina.Bianca);
            bool nereEsistono = PedineEsistono(TipoPedina.Nera);
            bool biancheHannoMosse = GetTutteLeMosse(TipoPedina.Bianca).Count > 0;
            bool nereHannoMosse = GetTutteLeMosse(TipoPedina.Nera).Count > 0;

            if (!nereEsistono || !nereHannoMosse)
                MessageBox.Show("Vince il Bianco! ");
            else if (!biancheEsistono || !biancheHannoMosse)
                MessageBox.Show("Vince il Nero! ");
        }

        //SUPPORTO 

        private bool ÈMia(Cella cella)
        {
            if (Scacchiera.TurnoCorrente == TipoPedina.Bianca)
                return cella.Pedina == TipoPedina.Bianca || cella.Pedina == TipoPedina.BiancaDama;
            else
                return cella.Pedina == TipoPedina.Nera || cella.Pedina == TipoPedina.NeraDama;
        }

        private bool ÈNemica(Cella cella, Cella miaPedina)
        {
            bool miaÈBianca = miaPedina.Pedina == TipoPedina.Bianca || miaPedina.Pedina == TipoPedina.BiancaDama;
            bool cellaÈBianca = cella.Pedina == TipoPedina.Bianca || cella.Pedina == TipoPedina.BiancaDama;
            bool cellaÈNera = cella.Pedina == TipoPedina.Nera || cella.Pedina == TipoPedina.NeraDama;

            if (miaÈBianca) return cellaÈNera;
            else return cellaÈBianca;
        }

        private bool ÈInBounds(int riga, int colonna)
        {
            return riga >= 0 && riga < 8 && colonna >= 0 && colonna < 8;
        }

        private bool PedineEsistono(TipoPedina colore)
        {
            TipoPedina dama = colore == TipoPedina.Bianca ? TipoPedina.BiancaDama : TipoPedina.NeraDama;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (Scacchiera.Griglia[r, c].Pedina == colore ||
                    Scacchiera.Griglia[r, c].Pedina == dama)
                        return true;
            return false;
        }

        private bool CiSonoCatture()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    Cella cella = Scacchiera.Griglia[r, c];
                    if (ÈMia(cella) && GetCatture(cella).Count > 0)
                        return true;
                }
            return false;
        }

        private List<Cella> GetTutteLeMosse(TipoPedina colore)
        {
            var tutte = new List<Cella>();
            TipoPedina dama = colore == TipoPedina.Bianca ? TipoPedina.BiancaDama : TipoPedina.NeraDama;

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    Cella cella = Scacchiera.Griglia[r, c];
                    if (cella.Pedina == colore || cella.Pedina == dama)
                        tutte.AddRange(GetMosseValide(cella));
                }
            return tutte;
        }

        private void CambiaTurno()
        {
            Scacchiera.TurnoCorrente = Scacchiera.TurnoCorrente == TipoPedina.Bianca
            ? TipoPedina.Nera
            : TipoPedina.Bianca;
        }
    }
}
