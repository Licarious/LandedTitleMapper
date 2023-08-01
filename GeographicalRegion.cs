using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LandedTitleMapper
{
    internal class GeographicalRegion
    {
        public string name = "";
        public HashSet<GeographicalRegion> subRegions = new();
        public HashSet<string> unknownSubRegions = new();
        public HashSet<Holding> holdings = new();
        public HashSet<Prov> provs = new();
        public HashSet<(int x, int y)> coords = new();
        public bool graphical = false;

        public Color color = Color.FromArgb(0, 0, 0, 0);
        public Color nameColor = Color.FromArgb(0, 0, 0, 0);

        public (int x, int y) center = (-1, -1);
        public (int h, int w) size = (-1, -1);

        public GeographicalRegion(string name) {
            this.name = name;
        }

        public void SetCoords() {
            foreach (Holding h in holdings) {
                h.SetCoords();
                coords.UnionWith(h.coords);
            }
            foreach (Prov p in provs) {
                coords.UnionWith(p.coords);
            }
            foreach (GeographicalRegion r in subRegions) {
                r.SetCoords();
                coords.UnionWith(r.coords);
            }
            
            
        }

        public void GetCenter() {
            //check if coords has elements
            if (coords.Count == 0) {
                return;
            }
            (center, size) = MaximumRectangle.Center(coords, false, false);
            /*
            //if size size.h or size.w is more than 5x the other the do it agin for square
            if (size.h > size.w * 5 || size.w > size.h * 5) {
                Console.WriteLine("switching " + name + " to square because" + size);
                (center, size) = MaximumRectangle.Center(coords, false, true);
            }
            */
        }

        public void SetColor() {
            //check if any subRegions has a color without alpha 0
            foreach (GeographicalRegion r in subRegions) {
                r.SetColor();
                if (r.color.A != 0) {
                    color = r.color;
                    return;
                }
            }
            //check if any holdings has a color without alpha 0
            foreach (Holding h in holdings) {
                if (h.color.A != 0) {
                    color = h.color;
                    return;
                }
            }
            //check if any provs has a color without alpha 0
            foreach (Prov p in provs) {
                if (p.color.A != 0) {
                    color = p.color;
                    return;
                }
            }
            //set color to white
            color = Color.FromArgb(255, 255, 255, 255);
        }

        public void SetNameColor(int distance = 150) {
            //find a color that is significantly different from the region's color within the region's subregions, holdings and provs
            foreach (GeographicalRegion gr in subRegions) {
                gr.SetNameColor();
                //if atleast 2 of the rgb values are different by more than distance, set nameColor to gr.color
                int count = 0;
                if (Math.Abs(gr.color.R - color.R) > distance) {
                    count++;
                }
                if (Math.Abs(gr.color.G - color.G) > distance) {
                    count++;
                }
                if (Math.Abs(gr.color.B - color.B) > distance) {
                    count++;
                }
                if (count >= 2) {
                    nameColor = gr.color;
                    return;
                }

            }
            foreach (Holding h in holdings) {
                //if atleast 2 of the rgb values are different by more than distance, set nameColor to h.color
                int count = 0;
                if (Math.Abs(h.color.R - color.R) > distance) {
                    count++;
                }
                if (Math.Abs(h.color.G - color.G) > distance) {
                    count++;
                }
                if (Math.Abs(h.color.B - color.B) > distance) {
                    count++;
                }
                if (count >= 2) {
                    nameColor = h.color;
                    return;
                }
            }
            foreach (Prov p in provs) {
                //if atleast 2 of the rgb values are different by more than distance, set nameColor to p.color
                int count = 0;
                if (Math.Abs(p.color.R - color.R) > distance) {
                    count++;
                }
                if (Math.Abs(p.color.G - color.G) > distance) {
                    count++;
                }
                if (Math.Abs(p.color.B - color.B) > distance) {
                    count++;
                }
                if (count >= 2) {
                    nameColor = p.color;
                    return;
                }
            }
            //add 100 to rgb values of color, loop if value supasses 255, and set nameColor to that
            int r = (color.R + distance)%255;
            int g = (color.G + distance)% 255;
            int b = (color.B + distance)% 255;
            nameColor = Color.FromArgb(255, r, g, b);

        }

    }
}
