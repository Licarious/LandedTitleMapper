using System.Drawing;

namespace LandedTitleMapper
{
    internal class Prov
    {
        public int id = -1;
        public string name = "";
        public Color color = Color.FromArgb(0, 0, 0, 0);
        //hash set for storing (x,y) coordinates of the province pixels
        public HashSet<(int x, int y)> coords = new();
        //type of province (sea, river, lake, waseland, etc)
        public string type = "";
        
        public Prov(int id, int red, int green, int blue, string name) {
            this.id = id;
            this.name = name;
            color = Color.FromArgb(255, red, green, blue);
        }

        //tostring method for printing the province
        public override string ToString() {
            return $"{id} {name} {color} {type}";
        }

        

    }
}
