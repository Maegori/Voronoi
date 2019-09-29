using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;


namespace Triangle
{
    public class MultiValueDictionary<Key, Value> : Dictionary<Key, List<Value>> {

        public void Add(Key key, Value value) {
            List<Value> values;
            if (!this.TryGetValue(key, out values)) {
                values = new List<Value>();
                this.Add(key, values);
            }
            values.Add(value);
        }
    }

    class Program
    {
        public static Bitmap new_img;
        public static int height;
        public static int width;
        public static int xlen = 0;
        public static int ylen = 0;
        public static Point[,] mat = new Point[1, 1];
        // this makes sure that all regions are the same size(not different at edges)
        public static int filler = 100;
        private static readonly Point[] p = {
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1),
            new Point(-1, 0),
            new Point(0, 0),
            new Point(1, 0),
            new Point(-1, 1),
            new Point(0, 1),
            new Point(1, 1)
        };

        public  static Dictionary<Point, List<int>> colors = new Dictionary<Point, List<int>>();
        public static MultiValueDictionary<Point, Point> vor = new MultiValueDictionary<Point, Point>();

        static void Main(string[] args)
        {
            Bitmap bmp;
            string file = "";
            string[] filename;

            do{
                Console.Write("Enter a valid filename: ");
                file = Console.ReadLine();
                filename = file.Split('.');
            } while(!(File.Exists(file)));

            bmp = new Bitmap(file);
            width = bmp.Width;
            height = bmp.Height;

            string dens = "1";
            do{
                Console.Write("Density(0 < int < 100): ");
                dens = Console.ReadLine();

            }while (!(0 < Int32.Parse(dens) && Int32.Parse(dens) <= 100));
            string NewFileName = filename[0] + dens + "." + filename[1];  

            List<String> options = new List<String> {"single", "gif", "debug"};
            string ops = "single";
            do{
                Console.Write("Options(only single available): ");
                ops = Console.ReadLine();
            }while (!(options.Contains(ops)));

            Tuple<int, int> ar = calculate_aspect(dens, width, height);
            decimal dxlen = Convert.ToDecimal(width + filler) / Convert.ToDecimal(ar.Item1);
            decimal dylen = Convert.ToDecimal(height + filler) / Convert.ToDecimal(ar.Item2);
            int xlen = Convert.ToInt32(Math.Round(dxlen));
            int ylen = Convert.ToInt32(Math.Round(dylen));

            Console.WriteLine();
            Console.WriteLine("========Diagnostics========");
            Console.WriteLine("Width, Height: {0}, {1}", width, height);
            Console.WriteLine("Regions in (x,y) directions: {0} {1}", xlen, ylen);
            Console.WriteLine("# Voronoi Sites: {0}", xlen * ylen);
            Console.WriteLine("Cell Size(x, y): {0}, {1}", ar.Item1, ar.Item2);


            if ( ops == "single"){
                new_img = voroniser(bmp, dens, ar, xlen, ylen);
            }
            else if (ops == "gif"){
                Bitmap[] bmp_array = new Bitmap[1];

                for (int i = 0; i < 1; i++){
                    Console.WriteLine("{0}/50", i + 1);
                    bmp_array[i] = voroniser(bmp, dens, ar, xlen, ylen);
                }
                
                Console.WriteLine("Creating GIF...");                
            }
            else if (ops == "debug"){
                Bitmap temp_img = voroniser(bmp, dens, ar, xlen, ylen);
                new_img = debug_painter(temp_img);
            }
            
            Directory.CreateDirectory("output");
            new_img.Save(@"output\" + NewFileName);
            Console.WriteLine();

            Console.WriteLine("All done, image written to {0}", NewFileName);
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();

        }

        static Bitmap voroniser(Bitmap bmp, string dens, Tuple<int, int> ar, int xlen, int ylen)
        {
            Point[,] mat = point_generator(ar, xlen, ylen);
            Tuple<MultiValueDictionary<Point, Point>, Dictionary<Point, List<int>>> dicts = voronoi(bmp, mat, ar, width, height, xlen, ylen);
            MultiValueDictionary<Point, Point> vor = dicts.Item1;
            Dictionary<Point, List<int>> colors = dicts.Item2;

            Bitmap img = new Bitmap(Convert.ToInt32(width), Convert.ToInt32(height));
            img = painter(img, vor, colors);

            return img;
        }

        //returns a tweaked aspect ratio which determines the size of each cell in the voronoi matrix(mat)
        static Tuple<int, int> calculate_aspect(string dens, int width, int height)
        {
            Decimal res = Decimal.Parse(dens);

            decimal Dec_x = Convert.ToDecimal(width + filler) / Convert.ToDecimal(100);
            decimal Dec_y = Convert.ToDecimal(height + filler) / Convert.ToDecimal(100);
            decimal x = Math.Ceiling(Dec_x);
            decimal y = Math.Ceiling(Dec_y);

            return Tuple.Create(Convert.ToInt32(100 / res * x) ,Convert.ToInt32(100 / res * y));
        }


        //returns an array representing the image containing the voronoi sites
        static Point[,] point_generator(Tuple<int, int> ar, int xlen, int ylen)
        {
            int height_c = 0;
            Point[,] mat = new Point[ylen + 1, xlen + 1];
            Random rnd = new Random();
            int x_offset = ar.Item1;
            int y_offset = ar.Item2;

            //creates a random coordinate within the bounds of the array cell determined by the aspect ratio
            for (int y = 0; y < ylen + 1; y++){
                int width_c = 0;
                for (int x = 0; x < xlen + 1; x++){
                    int rnd_w = rnd.Next(width_c, width_c + x_offset);
                    int rnd_h = rnd.Next(height_c, height_c + y_offset);
                    Point vorsite = new Point(rnd_w, rnd_h);
                    mat[y, x] = vorsite;
                    width_c = x_offset * x;
                }
                height_c = y_offset * y;
            }
            return mat;
        }

        //returns the closest voronoi site to the pixel
        static Point closest_to(Point pixel, Point[,] mat, Tuple<int, int> ar, int xlen, int ylen)
        {
            /*
            looks like shit I know, but the coordinates of the area variable have to be
            rounded for a nice looking result. the regions will look too cubey otherwise.
            do you have better ways to round this?
            */
            //determines where the pixel is in the voronoi matrix(mat)
            decimal dareax = Convert.ToDecimal(pixel.X) / Convert.ToDecimal(ar.Item1);
            decimal dareay = Convert.ToDecimal(pixel.Y) / Convert.ToDecimal(ar.Item2);
            decimal rareax = Math.Round(dareax);
            decimal rareay = Math.Round(dareay);
            Point area = new Point(Decimal.ToInt32(rareax), Decimal.ToInt32(rareay));

            Point[] around = p;
            Point[] square = new Point[9];
            Stack<Point> close = new Stack<Point>(9);

            //creates an array of 9 values who point to coordinates in the voronoi matrix(mat)
            for (int i = 0; i < 9; i++){
                Point a = around[i];
                int x = around[i].X + area.X;
                int y = around[i].Y + area.Y;
                square[i] = new Point(x, y);
            }

            //creates a stack with the 9 closest voronoi regions from the voronoi matric(mat)
            for (int i = 0; i < 9; i++){
                if (square[i].X >= 0 && square[i].X < xlen && square[i].Y >= 0 && square[i].Y < ylen){
                    Point coor = (mat[square[i].Y, square[i].X]);
                    close.Push(coor);
                }
            }

            Point closest = close.Pop();
            int min_dist = GetDistanceSqr(closest, pixel);

            foreach (Point coor in close){
                int dist = GetDistanceSqr(coor, pixel);
                if (dist < min_dist){
                    min_dist = dist;
                    closest = coor;
                }
            }
            return closest;
        }

        static int GetDistanceSqr(Point p1, Point p2){
                int dx = p1.X - p2.X;
                int dy = p1.Y - p2.Y;
                return dx * dx + dy * dy;
            }

        //returns dictionaries linking each voronoi site to both its color and pixels
        static Tuple<MultiValueDictionary<Point, Point>, Dictionary<Point, List<int>>> voronoi(Bitmap bmp, Point[,] mat, Tuple<int, int> ar, int width, int height, int xlen, int ylen)
        {
            int r;
            int g;
            int b;

            for (int y = 0; y < height; y++){
                for (int x = 0; x < width; x++){
                    Point pixel = new Point(x, y);
                    Color color = bmp.GetPixel(x, y); 
                    Point region = closest_to(pixel, mat, ar, xlen, ylen);

                    vor.Add(region, pixel);

                    //if the voronoi site exists in the dictionary, throw the r, g, b values on the heap of that site
                    if (colors.ContainsKey(region)){
                        List<int> c = new List<int>(3);
                        c.Add(colors[region][0]); c.Add(colors[region][1]); c.Add(colors[region][2]); 
                        r = c[0] + color.R; g = c[1] + color.G; b = c[2] + color.B;
                        c[0] = r; c[1] = g; c[2] = b;
                        colors.Remove(region);
                        colors.Add(region, c);
                    }
                    else{
                        List<int> c = new List<int>(3);
                        c.Insert(0, color.R); c.Insert(1, color.G); c.Insert(2, color.B);
                        colors.Add(region, c);
                    }
                }
            }
            return Tuple.Create(vor, colors);
        }

        //returns the edited version of the image
        static Bitmap painter(Bitmap img, MultiValueDictionary<Point, Point> vor, Dictionary<Point, List<int>> colors){

            int r;
            int g;
            int b;

            //runs over all regions in the dictionaries to determine in which color to paint each pixel
            foreach (Point region in vor.Keys){
                int points = vor[region].Count;
                List<int> color = colors[region];
                List<Point> pixels = vor[region];

                r = color[0] / points; g = color[1] / points; b = color[2] / points;
                Color col = Color.FromArgb(r, g, b);

                foreach (Point pixel in pixels){
                    img.SetPixel(pixel.X, pixel.Y, col);
                }
            }

            return img;
        }

        static Bitmap debug_painter(Bitmap img)
        {
            Color col = Color.FromArgb(0, 0, 0);

            for (int y = 0; y < ylen + 1; y++){
                for (int x = 0; x < xlen + 1; x++){
                    Point pixel = mat[y, x];
                    img.SetPixel(pixel.X, pixel.Y, col);
                }
            }
            return img;
        }
    }
}
