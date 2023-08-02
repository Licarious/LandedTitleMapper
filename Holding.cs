using System.Drawing;

namespace LandedTitleMapper
{
    internal class Holding
    {
        public string name = "";
        public int provid = -1;
        public Color color = Color.FromArgb(0, 0, 0, 0);
        public HashSet<Holding> subHoldings = new();
        public Holding? parent = null;
        public Holding? capital = null;
        public string capitalName = "";
        public Prov? prov = null;
        public int level = -1;
        public HashSet<(int x, int y)> coords = new();

        public (int x, int y) center = (-1, -1);
        public (int h, int w) size = (-1, -1);

        public HashSet<(int x, int y)> border = new();

        public Holding(string name, Holding parent) {
            this.name = name;
            this.parent = parent;
        }
        public Holding(string name) {
            this.name = name;
        }

        //cascade down the holding tree and find ones where prov is not null and set coords to that prov's coords list
        public HashSet<(int x, int y)> SetCoords2() {
            if (prov != null) {
                coords = prov.coords;
            }
            foreach (Holding h in subHoldings) {
                coords.UnionWith(h.SetCoords2());
            }
            return coords;
        }

        //toString prints the holding and its subholdings names
        public void PrintNestedHolding(int level = 0) {
            if (prov != null) {
                Console.WriteLine(new string('\t', level) + name + " " + coords.Count + "  " + prov.id + " - " + prov.coords.Count);
            }
            else {
                Console.WriteLine(new string('\t', level) + name + " " + coords.Count);
            }

            

            //run the method for each subholding with increased level
            foreach (Holding h in subHoldings) {
                h.PrintNestedHolding(level + 1);
            }
        }

        public void GetCenter2(bool squareDefault = false) {
            //check if coords has elements
            if (coords.Count == 0) {
                return;
            }


            (center, size) = MaximumRectangle.Center(coords, false, squareDefault);

        }

    }
}
