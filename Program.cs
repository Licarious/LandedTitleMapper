using LandedTitleMapper;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

internal class Program
{
    private static void Main(string[] args) {
        //local dir go 3 levels up to get to the root of the project
        string? localDir = Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).FullName).FullName).FullName;
        Random? rnd = new();
        //stopwatch
        Stopwatch sw = Stopwatch.StartNew();

        string modName = "MPE July 30";
        bool splitSingleWord = true;
        List<string> geographicalGrouping = new();
        List<string> geographicalGroupingStartsWith = new();
        List<string> geographicalGroupingEndsWith = new();
        parseConfig();

        string[] levelList = { "e_", "k_", "d_", "c_" };
        Dictionary<string, Dictionary<Color, List<Holding>>> holdingColorLevels = new() {
            { "e_", new Dictionary<Color, List<Holding>>() },
            { "k_", new Dictionary<Color, List<Holding>>() },
            { "d_", new Dictionary<Color, List<Holding>>() },
            { "c_", new Dictionary<Color, List<Holding>>() }
        };

        HashSet<Prov> provSet = parseProvDefinition();
        ParseDefaultMap(provSet);
        
        parseMap2(provSet);
        drawBlankMap(provSet, modName);

        HashSet<Holding> holdingSet = ParseLandedTitles(provSet);
        
        holdingStats(holdingSet, modName, levelList);

        
        foreach (string level in levelList) {
            drawLandedTitles(holdingSet, level, modName);
            drawOutline(level, modName);
            WriteNames(holdingSet, level, modName);
        }

        bool drawWorldRegions = true;
        if (drawWorldRegions) {
            parseGeographicalRegions(holdingSet, provSet);
        }
        
        Console.WriteLine(sw.Elapsed+"s");
        

        HashSet<Prov> parseProvDefinition() {
            //prov hashset for storing all provinces
            HashSet<Prov> provSet = new();

            //read the strings in definition.csv file and store the provinces in the prov hashset
            string[] lines = File.ReadAllLines(localDir + @"\Input\map_data\definition.csv");
            foreach (string line in lines) {
                //if line strip starts with # continue
                if (line.TrimStart().StartsWith("#") || line.Trim().Length == 0) continue;
                //split line on ';' and the format is [id, red, green, blue, name]
                string[] split = line.Split(';');
                provSet.Add(new Prov(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]), split[4]));
            }
            return provSet;
        }
        
        void ParseDefaultMap(HashSet<Prov> provSet) {
            Console.WriteLine("Parsing default.map...");
            //read all string in default map in the Input folder
            string[] lines = File.ReadAllLines(localDir + @"\Input\map_data\default.map");

            //loop through all lines
            foreach (string line in lines) {
                string cl = CleanLine(line);
                if (cl.Length == 0) continue;

                //if cl contains RANGE or LIST
                GetRangeList(cl, provSet);
            }
        }

        void GetRangeList(string line, HashSet<Prov> provSet) {
            string type = line.Split("=")[0].Trim().ToLower();

            //if line contains RANGE
            if (line.ToUpper().Contains("RANGE")) {
                //split the line on { and }
                string[] parts = line.Split('{', '}')[1].Split();
                //get the first and last number in parts
                int first = -1;
                int last = -1;
                foreach (string part in parts) {
                    //try parse int
                    if (int.TryParse(part, out int num)) {
                        if (first == -1) first = num;
                        else last = num;
                    }
                }
                //loop through all numbers between first and last
                for (int i = first; i <= last; i++) {
                    //find prov with id i
                    foreach (Prov prov in provSet) {
                        if (prov.id == i) {
                            //treat impassable_seas as an end state
                            if (prov.type == "impassable_seas") { }
                            else if ((prov.type == "sea_zones" && (type == "wasteland" || type == "impassable_terrain"))
                                || (prov.type == "wasteland" || prov.type == "impassable_terrain") && type == "sea_zones") {
                                prov.type = "impassable_seas";
                            }
                            else {
                                //set type of prov
                                prov.type = type;
                            }
                            //print prov id and type
                            //Console.WriteLine(prov.id + " " + prov.type);
                        }
                    }
                }

            }
            else if (line.ToUpper().Contains("LIST")) {
                //split the line on { and }
                string[] parts = line.Split('{', '}')[1].Split();
                //loop through all parts
                foreach (string part in parts) {
                    //try parse int
                    if (int.TryParse(part, out int num)) {
                        //find prov with id num

                        foreach (Prov prov in provSet) {
                            if (prov.id == num) {
                                //treat impassable_seas as an end state
                                if (prov.type == "impassable_seas") { }
                                else if ((prov.type == "sea_zones" && (type == "wasteland" || type == "impassable_terrain"))
                                || (prov.type == "wasteland" || prov.type == "impassable_terrain") && type == "sea_zones") {
                                    prov.type = "impassable_seas";
                                }
                                else {
                                    //set type of prov
                                    prov.type = type;
                                }
                                //print prov id and type
                                //Console.WriteLine(prov.id + " " + prov.type);
                            }
                        }
                    }
                }
            }

        }

        void parseMap2(HashSet<Prov> provSet) {
            Bitmap? provMap = new(localDir + @"\Input\map_data\provinces.png");

            Console.WriteLine("Prossessing Provs:");

            //create a dictionary for the color and prov object
            Dictionary<Color, Prov> colorProvDict = new();
            foreach (Prov prov in provSet) {
                //if prov is already in the dictionary replace it with the new prov
                if (colorProvDict.ContainsKey(prov.color)) colorProvDict[prov.color] = prov;
                //if prov is not in the dictionary add it
                else colorProvDict.Add(prov.color, prov);
            }

            //parallel assigne coords to provs using provMapList and colorProvDict
            int count = 0;

            for (int y = 0; y < provMap.Height; y++) {
                for (int x = 0; x < provMap.Width; x++) {
                    Color c = provMap.GetPixel(x, y);
                    if (!colorProvDict.ContainsKey(c)) continue;
                    colorProvDict[c].coords.Add((x, y));
                }
                //print progress every 10%
                count++;
                if (count % (provMap.Height / 10) == 0) Console.WriteLine("\t" + (count / (provMap.Height / 10)) * 10 + "%\t" + sw.Elapsed + "s");
            }

        }
        
        HashSet<Holding> ParseLandedTitles(HashSet<Prov> provSet) {
            string[] titleStart = { "e_", "k_", "d_", "c_", "b_" };
            HashSet<Holding> holdingSet = new();

            //read all txt files in landed_titles folder
            string[] files = Directory.GetFiles(localDir + @"\Input\landed_titles\", "*.txt", SearchOption.AllDirectories);

            foreach (string file in files) {
                //read the lines in the file
                string[] lines = File.ReadAllLines(file);

                Holding? currentHolding = null;
                int holdingDepth = 0;
                //stack for storing the parrent holdings
                Stack<Holding> parrentHoldings = new();
                parrentHoldings.Push(null);

                int indentation = 0;

                foreach (string line in lines) {
                    string cl = CleanLine(line);

                    //if line is empty or a comment skip it
                    if (cl == "") continue;

                    //if line starts with a titleStart
                    if (titleStart.Any(s => cl.StartsWith(s))) {
                        string tmpName = cl.Split('=')[0].Trim();

                        //if a holding is already named tmpName grab it
                        if (holdingSet.Any(h => h.name == tmpName)) {
                            currentHolding = holdingSet.First(h => h.name == tmpName);
                            Console.WriteLine("Editing: " + currentHolding.name);
                        }
                        //else create a new holding
                        else {
                            currentHolding = new Holding(tmpName, parrentHoldings.Count > 0 ? parrentHoldings.Peek() : null);
                            holdingSet.Add(currentHolding);
                        }
                        holdingDepth = indentation;
                        parrentHoldings.Peek()?.subHoldings.Add(currentHolding);
                        parrentHoldings.Push(currentHolding);

                    }
                    
                    if (currentHolding != null) {
                        //if line starts with a color assign the color to the current holding
                        if (cl.StartsWith("color")) {
                            ColorToHolding(currentHolding, cl);

                        }
                        //if line starts with province
                        else if (cl.StartsWith("province")) {
                            List<int> idList = new();
                            foreach (string s in cl.Split("=")[1].Trim().Split()) {
                                if (int.TryParse(s, out int id)) idList.Add(id);
                            }
                            try {
                                currentHolding.prov = provSet.First(p => p.id == idList[0]);
                                currentHolding.coords = currentHolding.prov.coords;
                                //Console.WriteLine("Assigning " + currentHolding.name + " to " + currentHolding.prov.name);
                            }
                            catch {
                                Console.WriteLine("Error: could not find prov with id " + idList[0] + " for holding " + currentHolding.name + "\nin file " + file.Split('\\', '/')[^1] + "\nDouble check if that file should be part of your game/mod(s)\n");

                            }
                        }

                        //if line starts with capital
                        else if (cl.StartsWith("capital")) {
                            currentHolding.capitalName = cl.Split("=")[1].Trim();
                        }

                    }


                    //update indentation
                    if (cl.Contains('{') || cl.Contains('}')) {
                        string[] words = cl.Split();
                        foreach(string word in words) {
                            if (word.Contains('{')) indentation++;
                            else if (word.Contains('}')) {
                                indentation--;
                                if (indentation <= holdingDepth) {
                                    if (parrentHoldings.Count > 1) {
                                        //Console.WriteLine("\tPopping " + parrentHoldings.Peek().name);
                                        parrentHoldings.Pop();
                                        if (parrentHoldings.Count > 1) {
                                            currentHolding = parrentHoldings.Peek();
                                            holdingDepth = indentation;
                                        }
                                        else {
                                            currentHolding = null;
                                            holdingDepth = 0;
                                        }
                                    }
                                }

                            }

                        }

                    }
                }
            }

            //for all holdings where capitalName is not "" find the capital holding with that name and set the capital to that holding
            foreach (Holding h in holdingSet) {
                try {
                    if (h.capitalName != "") {
                        h.capital = holdingSet.First(h1 => h1.name == h.capitalName);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Error finding capital for holding " + h.name + " " + e.Message + " likely not assigned " + h.capitalName + " yet");
                }

            }

            

            //for all top level holdings setCoords2
            foreach (Holding h in holdingSet.Where(h => h.parent == null)) {
                h.SetCoords2();
                //h.PrintNestedHolding();
            }

            //if any holding in the holdingSet has has a color with an alpha value of 0
            if (holdingSet.Any(h => h.color.A == 0)) {
                //print all holdings with an alpha value of 0
                foreach (Holding h in holdingSet.Where(h => h.color.A == 0)) {
                    ColorToHolding(h, "");
                }
            }

            return holdingSet;
        }

        //assigne color to holding
        void ColorToHolding(Holding currentHolding, string line) {
            //if line is empty check if holding has any subholdings if it does grab the color from the first subholding and assign it to holding
            if (line == "") {
                if (currentHolding.subHoldings.Count > 0) currentHolding.color = currentHolding.subHoldings.First().color;
                //else check if the holding is a barony and assign the prov color to it
                else if (currentHolding.name.StartsWith("b_") && currentHolding.prov != null) currentHolding.color = currentHolding.prov.color;
            }

            else {
                //if hsv in the line color is an hsv color else it is an rgb color

                List<double> colorList = new();
                if (line.Contains('=')) {
                    foreach (string s in line.Split("=")[1].Trim().Split()) {
                        if (s == "#") break;
                        //try parse int
                        if (double.TryParse(s, out double i)) {
                            // if i is outside of the range 0-255 set it to 0 or 255
                            if (i < 0) i = 0;
                            else if (i > 255) i = 255;
                            colorList.Add(i);
                        }
                    }
                }
                else { Console.WriteLine("no = in line staring with color: " + line); }

                if (line.Split("#")[0].ToLower().Contains("hsv")) {
                    //color is hsv color
                    currentHolding.color = ColorFromHSV(colorList[0], colorList[1], colorList[2]);
                }
                else {
                    //if colorList has less than 3 elements add random values to it until it has 3 elements
                    while (colorList.Count < 3) colorList.Add(rnd.Next(0, 255));
                    //currentHolding color convert colorList to int
                    List<int> colorListInt = new();
                    foreach (double d in colorList) colorListInt.Add((int)d);

                    currentHolding.color = Color.FromArgb(colorListInt[0], colorListInt[1], colorListInt[2]);
                }


                

            }
            
            
            //level fisrt 2 chars of currentHolding name
            string level = currentHolding.name[..2].ToLower();

            if (holdingColorLevels.ContainsKey(level)) {
                if (holdingColorLevels[level].ContainsKey(currentHolding.color)) holdingColorLevels[level][currentHolding.color].Add(currentHolding);
                else holdingColorLevels[level].Add(currentHolding.color, new List<Holding>() { currentHolding });
                //Console.WriteLine("Adding " + currentHolding.name + " to " + level + " with color " + currentHolding.color);
            }

            else if (level.StartsWith("b_")) { }

            else Console.WriteLine("level not in holdingColorLevels: " + level);
            
        }


        //drawLandedTitles takes holding hashset, holdingLevel string, modName string
        //holdingLevel can be "e" for empire, "k" for kingdom, "d" for duchy, "c" for county, or "b" for barony
        //modName is the name of the mod
        //color is the color holding will be drawn in if it is not null
        //drawLandedTitles returns nothing
        void drawLandedTitles(HashSet<Holding> holdings, string holdingLevel, string modName) {
            Console.WriteLine("Drawing " + holdingLevel + ":");

            //if folder does not exist create it
            if (!Directory.Exists(localDir + @"\Output\" + modName + @"\Color Map\")) Directory.CreateDirectory(localDir + @"\Output\" + modName + @"\Color Map\");

            //open provinces.png from localDir input
            Bitmap? bmp = new(localDir + @"\Input\map_data\provinces.png");

            //new clear image with the same size of provinces.png
            Bitmap? newBmp = new(bmp.Width, bmp.Height);

            HashSet<Color> newColors = new();
            //sightly randomize colors when there are multiple holdings at the same level with the same color
            //go through each sub dictionary in holdingColorLevels
            foreach (Dictionary<Color, List<Holding>> holdingColors in holdingColorLevels.Values) {
                //if there are any colors with more than one holding in holdingColors
                foreach (List<Holding> holdingList in holdingColors.Values.Where(h => h.Count > 1)) {
                    //if there is >= 1 holdings with coordnates in holdingList then continue
                    int count = 0;
                    foreach (Holding h in holdingList) if (h.coords.Count>1) count++;

                    if (count <= 1) continue;

                    //Console.WriteLine("\tThere are " + count + " holdings with the same color " + holdingList[0].color + " starting with holding: " + holdingList[0].name);

                    //for each holding with coordinates in holdingList past the first one
                    for (int i = 0; i < holdingList.Count; i++) {
                        if (holdingList[i].coords.Count > 1) {
                            //find a similar color that is not in holdingColors and set the color of holdingList[i] to that color
                            Color newColor = holdingList[i].color;
                            int dist = 20;
                            int colorCount = 0;
                            
                            while (holdingColors.ContainsKey(newColor) && newColors.Contains(newColor)) {
                                //generate a random color that is +- dist from holdingList[i].color with min and max of 0 and 255
                                newColor = Color.FromArgb(
                                    Math.Clamp(holdingList[i].color.R + rnd.Next(-dist, dist), 0, 255),
                                    Math.Clamp(holdingList[i].color.G + rnd.Next(-dist, dist), 0, 255),
                                    Math.Clamp(holdingList[i].color.B + rnd.Next(-dist, dist), 0, 255)
                                    );

                                colorCount++;
                                if (colorCount % 5 == 0) {
                                    dist++;
                                    colorCount = 0;
                                }

                            }
                            
                            //set the color of holdingList[i] to newColor
                            holdingList[i].color = newColor;
                            //Console.WriteLine("\t\t" + holdingList[i].name + " color changed to " + newColor);

                            newColors.Add(holdingList[i].color);
                            

                        }
                        
                        
                    }

                }

            }

            

            //for each holding in holdings if holding name startswith holdingLevel, then set each pixel in holding.coords to the holding color
            foreach (Holding h in holdings.Where(h => h.name.StartsWith(holdingLevel))) {

                //for each coord in h.coords
                foreach ((int x, int y) in h.coords) {
                    //set the pixel at c.x, c.y to h.color
                    newBmp.SetPixel(x, y, h.color);
                }

            }

            //save newBmp to localDir + "\\Output\\" + holdingLevel + modName + ".png"
            newBmp.Save(localDir + @"\Output\" + modName + @"\Color Map\" + holdingLevel + modName + ".png");
        }

        void drawOutline(string holdingLevel, string modName) {
            //if folder does not exist create it
            if (!Directory.Exists(localDir + @"\Output\" + modName + @"\Outline\")) Directory.CreateDirectory(localDir + @"\Output\" + modName + @"\Outline\");

            //open localDir + "\\Output\\" + holdingLevel + modName + ".png" as bmp
            Bitmap? bmp = new(localDir + @"\Output\" + modName + @"\Color Map\" + holdingLevel + modName + ".png");

            //new clear image with the same size of provinces.png
            Bitmap? borderBmp = new(bmp.Width, bmp.Height);

            //go through each pixel in each row of bmp
            //if the pixel changes color then set the pixel to black
            foreach (int x in Enumerable.Range(0, bmp.Width - 1)) {
                foreach (int y in Enumerable.Range(0, bmp.Height)) {
                    if (bmp.GetPixel(x, y) != bmp.GetPixel(x + 1, y)) {
                        bmp.SetPixel(x, y, Color.Black);
                        borderBmp.SetPixel(x, y, Color.Black);
                    }
                }
            }

            //similary for each column
            foreach (int y in Enumerable.Range(0, bmp.Height-1)) {
                foreach (int x in Enumerable.Range(0, bmp.Width)) {
                    if (bmp.GetPixel(x, y) != bmp.GetPixel(x, y + 1)) {
                        bmp.SetPixel(x, y, Color.Black);
                        borderBmp.SetPixel(x, y, Color.Black);
                    }
                }
            }

            //save bmp to localDir + "\\Output\\" + holdingLevel + modName + "Outline.png"
            bmp.Save(localDir + "\\Output\\" + modName + @"\" + holdingLevel + modName + ".png");
            borderBmp.Save(localDir + @"\Output\" + modName + @"\Outline\" + holdingLevel + modName + "_Outline.png");

        }

        void drawBlankMap(HashSet<Prov> provSet, string modName) {
            Console.WriteLine("Drawing BlankMap:");

            outlineBarony(modName);
                
            //if folder does not exist create it
            if (!Directory.Exists(localDir + @"\Output\" + modName + @"\Blank Map\")) Directory.CreateDirectory(localDir + @"\Output\" + modName + @"\Blank Map\");

            //colors 
            Color impassable_seas = Color.DarkBlue;
            Color sea_zones = Color.Blue;
            Color river_provinces = Color.CornflowerBlue;
            Color lakes = Color.LightBlue;
            Color impassable_mountains = Color.FromArgb(64,64,64);

            //list for mapping color to string for output
            List<(Color, string)> colorList = new() {
                (impassable_seas, "impassable_seas"),
                (sea_zones, "sea_zones"),
                (river_provinces, "river_provinces"),
                (lakes, "lakes"),
                (impassable_mountains, "impassable_mountains")
            };



            //open provinces.png from localDir input
            Bitmap? bmp = new(localDir + @"\Input\map_data\provinces.png");

            //new white image with the same size of provinces.png
            int width = bmp.Width;
            int height = bmp.Height;
            Bitmap? blankBmp = new(width, height);
            //set all pixels to white
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    blankBmp.SetPixel(x, y, Color.White);
                }
            }

            //for each prov in provSet if type is in colorList then set each pixel in prov.coords to the color
            foreach (Prov p in provSet) {
                foreach ((Color, string) c in colorList) {
                    if (p.type == c.Item2) {
                        foreach ((int x, int y) in p.coords) {
                            blankBmp.SetPixel(x, y, c.Item1);
                        }
                    }
                }
            }
            //save newBmp to localDir + "\\Output\\" + holdingLevel + modName + ".png"
            blankBmp.Save(localDir + @"\Output\" + modName + @"\Blank Map\" + modName + ".png");


            //merge blankBmp with outlineBmp
            Bitmap? outlineBmp = new(localDir + @"\Output\" + modName + @"\Outline\b_" + modName + "_Outline.png");
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    if (outlineBmp.GetPixel(x, y).A != 0) {
                        blankBmp.SetPixel(x, y, Color.Black);
                    }
                }
            }

            blankBmp.Save(localDir + @"\Output\" + modName + @"\0_" + modName + "_BlankMap.png");

        }

        void outlineBarony(String modName) {
            //check if folder exists
            if (!Directory.Exists(localDir + @"\Output\" + modName + @"\Outline\")) Directory.CreateDirectory(localDir + @"\Output\" + modName + @"\Outline\");

            //open porvince.png from Input
            Bitmap? bmp = new(localDir + @"\Input\map_data\provinces.png");

            //new clear image with the same size of provinces.png
            Bitmap? borderBmp = new(bmp.Width, bmp.Height);
            
            //go through each pixel in each row of bmp
            //if the pixel changes color then set the pixel to black
            foreach (int x in Enumerable.Range(0, bmp.Width - 1)) {
                foreach (int y in Enumerable.Range(0, bmp.Height)) {
                    if (bmp.GetPixel(x, y) != bmp.GetPixel(x + 1, y)) {
                        //bmp.SetPixel(x, y, Color.Black);
                        borderBmp.SetPixel(x, y, Color.Black);
                    }
                }
            }

            //similary for each column
            foreach (int y in Enumerable.Range(0, bmp.Height - 1)) {
                foreach (int x in Enumerable.Range(0, bmp.Width)) {
                    if (bmp.GetPixel(x, y) != bmp.GetPixel(x, y + 1)) {
                        //bmp.SetPixel(x, y, Color.Black);
                        borderBmp.SetPixel(x, y, Color.Black);
                    }
                }
            }

            //save bmp to localDir + "\\Output\\" + holdingLevel + modName + "Outline.png"
            borderBmp.Save(localDir + "\\Output\\" + modName + @"\Outline\b_" + modName + "_Outline.png");
            borderBmp.Dispose();
        }

        void WriteNames(HashSet<Holding> holdings, string holdingLevel, string modName) {
            //load merged image from localDir + "\\Output\\" + modName+ "\\" + holdingLevel + modName + ".png"
            Bitmap? newBmp = new(localDir + @"\Output\" + modName + @"\" + holdingLevel + modName+ ".png");

            Graphics g = Graphics.FromImage(newBmp);
            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            PrivateFontCollection privateFontCollection = new();
            privateFontCollection.AddFontFile(localDir + @"\Input\Paradox_King_Script.otf"); //font for holding names


            float sizeMult = 1.0f;

            int minFontSize = 7;
            if (holdingLevel.StartsWith("c")) minFontSize = 4;

            //write holding names to map
            foreach (Holding h in holdings.Where(h => h.name.StartsWith(holdingLevel))) {
                h.GetCenter2();

                if (h.center == (-1, -1)) continue;
                
                string[] words = h.name.Split("_");
                //get all but first element of words and join them with " "
                string name = string.Join(" ", words.Skip(1)).Trim();
                


                Font font1 = new(privateFontCollection.Families[0], minFontSize);
                //reduce linespacing by 20% for font1



                //calculate the size of the string
                SizeF stringSize = g.MeasureString(name, font1);
                
                
                //increase the size of the string until it fills the holding
                while (stringSize.Width < (h.size.h * sizeMult) && stringSize.Height < (h.size.w * sizeMult)) {
                    font1 = new Font(privateFontCollection.Families[0], font1.Size + 1);
                    stringSize = g.MeasureString(name, font1);
                }
                //increse font by 20% if font2
                font1 = new Font(privateFontCollection.Families[0], font1.Size * 1.2f);

                if (name.Split().Length > 1) {
                    string name2 = string.Join("\n", words.Skip(1)).Trim();
                    //Console.WriteLine("\n"+name2+"\n");
                    Font font2 = new(privateFontCollection.Families[0], minFontSize);
                    SizeF stringSize2 = g.MeasureString(name2, font2);

                    while (stringSize2.Width < (h.size.h * sizeMult) && stringSize2.Height < (h.size.w * sizeMult)) {
                        font2 = new Font(privateFontCollection.Families[0], font2.Size + 1);
                        stringSize2 = g.MeasureString(name2, font2);
                    }
                    //increse font by 20% if font2
                    font2 = new Font(privateFontCollection.Families[0], font2.Size * 1.2f);
                    //if area covered by font2 is greater than area covered by font1 then use font2
                    if (font2.Size > font1.Size * 1.2) {
                        font1 = font2;
                        stringSize = stringSize2;
                        name = name2;
                    }

                }
                //if only one work check if spliting into 2 lines will make text bigger
                else if (name.Split().Length == 1 && splitSingleWord) {
                    string name2 = string.Concat(words[1].AsSpan(0, words[1].Length / 2), "•\n", words[1].AsSpan(words[1].Length / 2));
                    Font font2 = new(privateFontCollection.Families[0], minFontSize);
                    SizeF stringSize2 = g.MeasureString(name2, font2);
                    while (stringSize2.Width < (h.size.h * sizeMult) && stringSize2.Height < (h.size.w * sizeMult)) {
                        font2 = new Font(privateFontCollection.Families[0], font2.Size + 1);
                        stringSize2 = g.MeasureString(name2, font2);
                    }
                    //increse font by 20% if font2
                    font2 = new Font(privateFontCollection.Families[0], font2.Size * 1.2f);
                    
                    //if font size for font2 is greater than a value of font1 size
                    if (font2.Size > font1.Size*font1.Size*0.15f) {
                        font1 = font2;
                        stringSize = stringSize2;
                        name = name2;
                    }
                }

                //invert the color of the holding color
                Color textColor = Color.FromArgb(255 - h.color.R, 255 - h.color.G, 255 - h.color.B);

                //if the 2 colors are too close to eachother
                int colorClosness = 75;
                if (Math.Abs(textColor.R - h.color.R) < colorClosness && Math.Abs(textColor.G - h.color.G) < colorClosness && Math.Abs(textColor.B - h.color.B) < colorClosness) {
                    //rotate the color of the text 120degrees around the color wheel
                    textColor = Color.FromArgb(h.color.R, (h.color.G + 85) % 255, (h.color.B + 170) % 255);
                    //Console.WriteLine("\tRoating color of " + h.name);
                }

                //write holding name to map centered at h.center with h.color and font Arial 12 bold
                g.DrawString(name, font1, new SolidBrush(textColor), new Point(h.center.x, h.center.y), stringFormat);

            }

            newBmp.Save(localDir + @"\Output\" + modName + @"\" + holdingLevel + modName + "_names.png");

        }

        //how dark is the color
        float rgbToYIQ(Color c) {
            return (c.R * 299 + c.G * 587 + c.B * 114) / 1000;
        }

        void holdingStats(HashSet<Holding> holdings, string modName, string[] levelList) {
            //write holdings satistics to file

            //check if folder exists
            if (!Directory.Exists(localDir + @"\Output\" + modName + @"\Holding Stats\")) Directory.CreateDirectory(localDir + @"\Output\" + modName + @"\Holding Stats\");
            //create file
            StreamWriter? sw = new(localDir + @"\Output\" + modName + @"\Holding Stats\" + modName + "_HoldingStats.csv");

            //write header
            sw.WriteLine("Holding Level;Count;Total;Min;Q1;Average;Q3;Max;Number of SubHolding;That Count");

            //for each holding level
            foreach (string level in levelList) {
                //get list of holdings of that level
                List<Holding> holdingList = holdings.Where(h => h.name.StartsWith(level)).ToList();

                //remove any holdings that dont have subholdings
                holdingList.RemoveAll(h => h.subHoldings.Count == 0);

                //if levelList is not c_
                if (level != "c_") {
                    //check all subholdings of each holding and remove any that dont have subholdings
                    List<Holding> removeList = new();
                    foreach (Holding h in holdingList) {
                        foreach (Holding subHolding in h.subHoldings) {
                            if (subHolding.subHoldings.Count == 0) {
                                removeList.Add(subHolding);
                            }
                        }
                    }
                    foreach (Holding h in removeList) {
                        holdingList.Remove(h);
                    }
                }

                //set for storing number of holdings with the same number of subholdings
                Dictionary<int, int> countDict = new();

                //for each holding
                foreach (Holding h in holdingList) {
                    //if countDict contains the number of subholdings then add 1 to the value
                    if (countDict.ContainsKey(h.subHoldings.Count)) {
                        countDict[h.subHoldings.Count] += 1;
                    }
                    //else add the number of subholdings as a key and set the value to 1
                    else {
                        countDict.Add(h.subHoldings.Count, 1);
                    }
                }

                //sort countDict by key
                countDict = countDict.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);



                //get count
                int count = holdingList.Count;

                //get total
                int total = holdingList.Sum(h => h.subHoldings.Count);

                //get min
                int min = holdingList.Min(h => h.subHoldings.Count);

                //get q1
                int q1 = holdingList.OrderBy(h => h.subHoldings.Count).Skip(count / 4).First().subHoldings.Count;

                //get average
                double average = holdingList.Average(h => h.subHoldings.Count);

                //get q3
                int q3 = holdingList.OrderBy(h => h.subHoldings.Count).Skip(count * 3 / 4).First().subHoldings.Count;

                //get max
                int max = holdingList.Max(h => h.subHoldings.Count);

                //write to file
                sw.WriteLine(level + ";" + count + ";" + total + ";" + min + ";" + q1 + ";" + average + ";" + q3 + ";" + max);

                //write countDict to file                
                foreach (KeyValuePair<int, int> kvp in countDict) {
                    sw.WriteLine(";;;;;;;;"+kvp.Key + ";" + kvp.Value);
                }
            }

            //close file
            sw.Close();

            


        }

        void parseGeographicalRegions(HashSet<Holding> holdings, HashSet<Prov> provSet) {
            //if geographical_regions folder does not exist then return
            if (!Directory.Exists(localDir + @"\Input\map_data\geographical_regions")) return;

            //read all .txt files in the geographical_regions folder and parse them
            string[] files = Directory.GetFiles(localDir + @"\Input\map_data\geographical_regions\", "*.txt");
            Dictionary<string, GeographicalRegion> geographicalRegionsDict = new();
            string[] holdingLevel = new string[] { "counties", "duchies" };

            //convert holdings to a dictionary
            Dictionary<string, Holding> holdingsDict = holdings.ToDictionary(h => h.name, h => h);
            //convert provSet to a dictionary
            Dictionary<int, Prov> provDict = provSet.ToDictionary(p => p.id, p => p);


            //for each file
            foreach (string file in files) {
                //read all lines in file
                string[] lines = File.ReadAllLines(file);


                int indentation = 0;
                GeographicalRegion? currentRegion = null;
                bool holdingFound = false;
                bool subRegionFound = false;
                bool provFound = false;
                //for each line
                foreach (string line in lines) {
                    string cl = CleanLine(line);

                    if (cl.Length == 0) continue;

                    if (indentation == 0) {
                        if (cl.Contains('=')){
                            //create a new GeographicalRegion
                            string name = cl.Split('=')[0].Trim();
                            currentRegion = new GeographicalRegion(name);
                            if (geographicalRegionsDict.ContainsKey(name)) {
                                Console.WriteLine("Replacing GeographicalRegion: " + name);
                                GeographicalRegion replaced = geographicalRegionsDict[name];

                                //find all GeographicalRegions that have the replaced GeographicalRegion as in their subregion
                                List<GeographicalRegion> subRegionsWithReplaced = geographicalRegionsDict.Values.Where(gr => gr.subRegions.Contains(replaced)).ToList();

                                geographicalRegionsDict.Remove(name);

                                //add currentRegion to subregions of all subRegionsWithReplaced
                                foreach (GeographicalRegion gr in subRegionsWithReplaced) {
                                    gr.subRegions.Remove(replaced);
                                    gr.subRegions.Add(currentRegion);
                                }
                            }
                            geographicalRegionsDict.Add(name, currentRegion);
                        }
                    }

                    if (indentation == 1 && currentRegion != null) {
                        //if line starts with an element of holdingLevel
                        if (holdingLevel.Contains(cl.Split('=')[0].Trim())) {
                            holdingFound = true;
                        }
                        else if (cl.StartsWith("regions")) {
                            subRegionFound = true;
                        }
                        else if (cl.StartsWith("provinces")) {
                            provFound = true;
                        }
                        else if (cl.StartsWith("color")) {
                            List<int> color = new();
                            string[] colorStr = cl.Split('=')[1].Split(' ');
                            //try parse
                            foreach (string s in colorStr) {
                                if (int.TryParse(s, out int i))
                                    color.Add(i);
                            }

                            //if color is valid
                            if (color.Count == 3)
                                currentRegion.color = Color.FromArgb(255, color[0], color[1], color[2]);

                        }
                        else if (cl.StartsWith("graphical")) currentRegion.graphical = true;
                        else if (cl.StartsWith('}')) {

                        }
                        else {
                            Console.WriteLine("Unknown line: " + cl);
                        }

                    }

                    if (holdingFound && currentRegion != null) {
                        string[] potentialHoldings = cl.Split();
                        //for each potential holding
                        foreach(string pHolding in potentialHoldings) {
                            //if pHolding is in holdingsDict
                            if (holdingsDict.ContainsKey(pHolding)) { 
                                //add pHolding to currentRegion.holdings
                                currentRegion.holdings.Add(holdingsDict[pHolding]);
                            }
                        }
                    }
                    else if (subRegionFound && currentRegion != null) {
                        string[] potentialRegions = cl.Split();
                        //for each potential region
                        foreach (string pRegion in potentialRegions) { 
                            //if pRegion is in geographicalRegionsDict
                            if (geographicalRegionsDict.ContainsKey(pRegion)) {
                                //add pRegion to currentRegion.subRegions
                                currentRegion.subRegions.Add(geographicalRegionsDict[pRegion]);
                            }
                            else {
                                currentRegion.unknownSubRegions.Add(pRegion);
                            }
                        }
                    }
                    else if (provFound) {
                        //create a list of try parse ints from cl
                        List<int> provIds = new();
                        string[] potentialProvIds = cl.Split();
                        foreach (string pProvId in potentialProvIds) {
                            if (int.TryParse(pProvId, out int provId)) {
                                provIds.Add(provId);
                            }
                        }
                        //for each provId
                        foreach (int provId in provIds) {
                            //if provId is in provSet
                            if (provDict.ContainsKey(provId)) {
                                try {
                                    //add provSet[provId] to currentRegion.provs
                                    currentRegion.provs.Add(provDict[provId]);
                                }
                                catch {
                                    Console.WriteLine("Prov" + provId + " was not found in definition.csv\ndouble check that " + file.Split('\\', '/')[^1] + "is accessable by your game/mod(s)");
                                }
                            }
                        }

                    }


                    if (cl.Contains('{') || cl.Contains('}')) {
                        string[] split = cl.Split();
                        foreach (string s in split) {
                            if (s.Contains('{')) {
                                indentation++;
                            }
                            else if (s.Contains('}')) {
                                indentation--;
                                if (indentation == 1) {
                                    holdingFound = false;
                                    subRegionFound = false;
                                    provFound = false;
                                }
                            }
                        }
                    }
                }

            }

            //check if unknownSubRegions are in geographicalRegionsDict and add them to subRegions if they are
            foreach (GeographicalRegion gr in geographicalRegionsDict.Values) {
                foreach (string unknownSubRegion in gr.unknownSubRegions) {
                    if (geographicalRegionsDict.ContainsKey(unknownSubRegion)) {
                        gr.subRegions.Add(geographicalRegionsDict[unknownSubRegion]);
                    }
                }
                //if color has an alpha value of 0, run SetColor
                if (gr.color.A == 0) {
                    gr.SetColor();
                }
                if (gr.nameColor.A == 0) {
                    gr.SetNameColor();
                }
                if (gr.coords.Count == 0) {
                    gr.SetCoords();
                }
            }


            //open provinces.png
            Bitmap provinceMap = new(localDir + @"\Input\map_data\provinces.png");
            //create a new bitmap with the same size as provinceMap

            //check if folder eixts
            if (!Directory.Exists(localDir + @"\Output\" + modName + @"\Geographical Regions\")) {
                //if not, create it
                Directory.CreateDirectory(localDir + @"\Output\" + modName + @"\Geographical Regions\");
            }


            PrivateFontCollection privateFontCollection = new();
            privateFontCollection.AddFontFile(localDir + @"\Input\Paradox_King_Script.otf"); //font for holding names
            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            


            Dictionary<string, List<GeographicalRegion>> GRGroupsDict = new();
            //add string fp1 - fp9 to dictionary
            for (int i = 1; i < 10; i++) {
                GRGroupsDict.Add("dlc_fp" + i+"_", new List<GeographicalRegion>());
            }
            //add string ep1 - ep9 to dictionary
            for (int i = 1; i < 10; i++) {
                GRGroupsDict.Add("dlc_ep" + i + "_", new List<GeographicalRegion>());
            }
            //List<string> substrings = new() { "RICE_", "_innovation_", "buildings_", "de_jure_", "world_", "material_", "graphical_", "custom_", "sea_", "hunt_", "ghw_", "special_" };
            
            
            //add substrings to dictionary
            foreach (string substring in geographicalGrouping) {
                GRGroupsDict.Add(substring, new List<GeographicalRegion>());
            }
            //add string "default_" to dictionary
            GRGroupsDict.Add("default_", new List<GeographicalRegion>());

            //for each region in sortedGR find the first key that it's name contains in GRGroupsDict and add it to the list, if none of the keys are found add it to unknown_
            foreach (GeographicalRegion gr in geographicalRegionsDict.Values) {
                bool found = false;
                foreach (string key in GRGroupsDict.Keys) {
                    //if key is in geographicalGroupingStartsWith
                    if (geographicalGroupingStartsWith.Contains(key)) {
                        //if gr.name starts with key
                        if (gr.name.ToLower().StartsWith(key)) {
                            //add gr to GRGroupsDict[key]
                            GRGroupsDict[key].Add(gr);
                            found = true;
                            break;
                        }
                    }
                    else if (geographicalGroupingEndsWith.Contains(key)) {
                        //if gr.name ends with key
                        if (gr.name.ToLower().StartsWith(key)) {
                            //add gr to GRGroupsDict[key]
                            GRGroupsDict[key].Add(gr);
                            found = true;
                            break;
                        }
                    }

                    else if (gr.name.ToLower().Contains(key)) {
                        GRGroupsDict[key].Add(gr);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    GRGroupsDict["default_"].Add(gr);
                }
            }



            //for each entry in GRGroupsDict
            foreach (KeyValuePair<string, List<GeographicalRegion>> kvp in GRGroupsDict) {
                //if the list is not empty
                if (kvp.Value.Count > 0) {
                    Console.WriteLine("\n" + kvp.Key + ":\t\t" + sw.Elapsed +"s");
                    //sort sortedGR by coords.Count from highest to lowest and remove all entries with coords.Count == 0
                    kvp.Value.Sort((x, y) => y.coords.Count.CompareTo(x.coords.Count));
                    kvp.Value.RemoveAll(x => x.coords.Count == 0);

                    while (kvp.Value.Count > 0) {

                        List<GeographicalRegion> group = new();
                        GeographicalRegion currentGR = kvp.Value[0];
                        HashSet<(int x, int y)> groupCoords = new();
                        //Console.WriteLine("Starting new group with " + currentGR.name);

                        group.Add(currentGR);
                        kvp.Value.Remove(currentGR);
                        groupCoords.UnionWith(currentGR.coords);
                        for (int i = 0; i < kvp.Value.Count; i++) {
                            GeographicalRegion gr = kvp.Value[i];
                            if (!groupCoords.Overlaps(gr.coords)) {
                                groupCoords.UnionWith(gr.coords);
                                group.Add(gr);
                                kvp.Value.Remove(gr);
                                i--;
                            }
                        }
                        //print the names of the regions in the group
                        Console.WriteLine("\tGroup:");
                        foreach (GeographicalRegion gr in group) {
                            Console.WriteLine("\t\t" + gr.name);
                        }
                        Console.WriteLine();


                        //draw group
                        Bitmap grMap = new(provinceMap.Width, provinceMap.Height);
                        Graphics g = Graphics.FromImage(grMap);
                        //file name will be the largest region in the group
                        string fileName = group.OrderByDescending(gr => gr.coords.Count).First().name;
                        //draw all pixles
                        foreach (GeographicalRegion gr in group) {
                            foreach ((int x, int y) in gr.coords) {
                                grMap.SetPixel(x, y, gr.color);
                            }

                        }

                        //write names
                        foreach (GeographicalRegion gr in group) {

                            //if center is not set, set it
                            if (gr.center == (-1, -1)) {
                                gr.GetCenter();
                            }

                            

                            string[] split = gr.name.Replace("__", "_").Split("_");

                            int minFontSize = 10;
                            Font font1 = new(privateFontCollection.Families[0], minFontSize);

                            string nameLines = gr.name.Replace("_", " ").Replace("  ", " ");
                            float fontSize = minFontSize;
                            bool set = false;
                            //number of _ in the name
                            int numLines = nameLines.Count(c => c == ' ');
                            //Console.WriteLine(nameLines + " maxLines: " + numLines);
                            //run GetMaxNameSize with desiredLines between 1 and the number of _ in name
                            for (int i = (int)Math.Sqrt(numLines); i <= numLines + 1; i++) {
                                //get the max size of the name with i desired lines
                                (float tmpSize, string tmpName) = GetMaxNameSize(gr.name.Replace("_", " ").Replace("  ", " "), gr.size, g, font1, minFontSize, i);
                                //if tmpSize is larger than fontSize, set fontSize to tmpSize and nameLines to tmpName
                                if (tmpSize > fontSize) {
                                    fontSize = tmpSize;
                                    nameLines = tmpName;
                                    //Console.WriteLine("\n" + nameLines + "\nsize: " + fontSize + " with " + i + " lines\n");
                                    set = true;
                                }
                                //I dont want +6 word names on the same line
                                if (!set) {
                                    nameLines = tmpName;
                                    set = true;
                                }
                            }

                            if (gr.color == gr.nameColor) {
                                Console.WriteLine("\n"+gr.name + "\ncolor\t\t" + gr.color + "\nnameColor:\t" + gr.nameColor + "\n" + nameLines + "\n");
                                //set to random color
                                gr.nameColor = Color.FromArgb(255, rnd.Next(256), rnd.Next(256), rnd.Next(256));
                                Console.WriteLine("new nameColor:\t" + gr.nameColor);
                            }

                            font1 = new(privateFontCollection.Families[0], fontSize * 1.2f);

                            g.DrawString(nameLines, font1, new SolidBrush(gr.nameColor), new Point(gr.center.x, gr.center.y), stringFormat);
                            
                        }
                        
                        //save grMap
                        grMap.Save(localDir + @"\Output\" + modName + @"\Geographical Regions\" + fileName + ".png");
                    }
                }


            }
        }

        (float, string) GetMaxNameSize(string name, (int w, int h) size, Graphics g, Font font, int minimumFontSize, int desiredLines) {
            PrivateFontCollection privateFontCollection = new();
            privateFontCollection.AddFontFile(localDir + @"\Input\Paradox_King_Script.otf"); //font for holding names
            StringFormat stringFormat = new() {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            
            //capitalize the first leter of each word in the name
            string[] split2 = name.Split();
            

            //subdivide the name into desiredLines parts
            List<string> split3 = new();

            //join find the lengh of each part of the name2 and merge them into desiredLines parts such that order is maintained and lengths are similar
            int totalLength = 0;
            int totalParts = 0;
            for (int i = 0; i < split2.Length; i++) {
                string s = split2[i];
                int length = s.Length;
                totalLength += length;
                totalParts++;
                if (totalParts == desiredLines) {
                    int tmpLength = i - totalParts + 1;
                    split3.Add(string.Join(" ", split2[tmpLength..(i + 1)]));
                    totalLength = 0;
                    totalParts = 0;
                }
            }

            //if there are any remaining parts, add them to the last part
            if (totalParts > 0) {
                split3[^1] += " " + string.Join(" ", split2[(split2.Length - totalParts)..]);
            }



            //join spit3 with \n
            string name3 = String.Join("\n", split3).Trim();
            //Console.WriteLine("\n"+name3+"\n");

            //Console.WriteLine(name3 + " has " + desiredLines + " lines because it has " + name3.Count(c => c == '\n') + " newLine chars");

            SizeF stringSize = g.MeasureString(name3, font);

            //find the largest font size that will fit in the holding
            while (stringSize.Width < (size.h) && stringSize.Height < (size.w)) {
                font = new Font(privateFontCollection.Families[0], font.Size + 0.5f);
                stringSize = g.MeasureString(name3, font);
                
            }
            

            return (font.Size, name3);
        }

        string CleanLine(string line) {
            return line.Replace("{", " { ").Replace("}"," } ").Replace("="," = ").Replace("  "," ").Split("#")[0].Trim();
        }

        void parseConfig() {
            //dictionary of all the config options (name: string, splitSingeWord: bool, geographicalGrouping: List<string>)
            string[] lines = File.ReadAllLines(localDir + @"\Input\settings.cfg");
            bool geographicalGroupingFound = false;
            
            foreach(string line in lines) {
                string cl = CleanLine(line);
                if (cl.StartsWith("name")) {
                    modName = cl.Split("=")[1].Replace("\"","").Trim();
                }
                else if (cl.StartsWith("splitSingleWord")) {
                    splitSingleWord = cl.Split("=")[1].Trim() == "true";
                }
                else if (cl.StartsWith("geographicalGrouping")) {
                    geographicalGroupingFound = true;
                    
                }

                if (geographicalGroupingFound) {
                    //split between { and } if there is a { or } on the line
                    string[] split = cl.Split("{");
                    if (split.Length > 1) {
                        string[] split2 = split[1].Split("}");
                        if (split2.Length > 1) {
                            //split between , if there is a , on the line
                            string[] split3 = split2[0].Split(",");
                            foreach (string s in split3) {
                                string startWithCheck = s.Split("\"")[0].Trim();
                                string[] parts = s.Split("\"");
                                try {
                                    if (parts[0].Contains('*')) {
                                        geographicalGroupingEndsWith.Add(s.Split("\"")[1].Trim().ToLower());
                                    }
                                    else if (parts[2].Contains('*')) {
                                        geographicalGroupingStartsWith.Add(s.Split("\"")[1].Trim().ToLower());
                                    }

                                }
                                catch {

                                }
                                //add each string to the geographicalGrouping list
                                geographicalGrouping.Add(s.Split("\"")[1].Trim().ToLower());
                            }
                        }
                    }
                    if (cl.Contains('}')) {
                        geographicalGroupingFound = false;
                    }
                }
            }
            

            //print out the config options
            Console.WriteLine("name: " + modName);
            Console.WriteLine("splitSingleWord: " + splitSingleWord);
            Console.WriteLine("geographicalGrouping: " + string.Join("  ", geographicalGrouping));
            Console.WriteLine("geographicalGroupingStartsWith: " + string.Join("  ", geographicalGroupingStartsWith));
            Console.WriteLine("geographicalGroupingEndsWith: " + string.Join("  ", geographicalGroupingEndsWith));
                


        }

    }

    private static Color ColorFromHSV(double v1, double v2, double v3) {
        //convert hsv to rgb
        double r, g, b;
        if (v3 == 0) {
            r = g = b = 0;
        }
        else {
            if (v2 == -1) v2 = 1;
            int i = (int)Math.Floor(v1 * 6);
            double f = v1 * 6 - i;
            double p = v3 * (1 - v2);
            double q = v3 * (1 - f * v2);
            double t = v3 * (1 - (1 - f) * v2);
            switch (i % 6) {
                case 0: r = v3; g = t; b = p; break;
                case 1: r = q; g = v3; b = p; break;
                case 2: r = p; g = v3; b = t; break;
                case 3: r = p; g = q; b = v3; break;
                case 4: r = t; g = p; b = v3; break;
                case 5: r = v3; g = p; b = q; break;
                default: r = g = b = v3; break;
            }
        }
        return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }
}