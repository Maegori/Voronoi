using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageMagick;


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

            List<String> options = new List<String> {"single", "gif"};
            string ops = "single";
            do{
                Console.Write("Options(only single available): ");
                ops = Console.ReadLine();
            }while (!(options.Contains(ops)));

            Tuple<int, int> ar = calculate_aspect(dens, width, height);
            decimal dxlen = Convert.ToDecimal(width) / Convert.ToDecimal(ar.Item1);
            decimal dylen = Convert.ToDecimal(height) / Convert.ToDecimal(ar.Item2);
            int xlen = Convert.ToInt32(Math.Round(dxlen));
            int ylen = Convert.ToInt32(Math.Round(dylen));

            Console.WriteLine();
            Console.WriteLine("========Diagnostics========");
            Console.WriteLine("Width, Height: {0}, {1}", width, height);
            Console.WriteLine("Regions in (x,y) directions: {0} {1}", xlen, ylen);
            Console.WriteLine("# Voronoi Sites: {0}", xlen * ylen);
            Console.WriteLine("Cell Size(x, y): {0}, {1}", ar.Item1, ar.Item2);

            MagickNET.SetTempDirectory("output");

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
                using (MagickImageCollection collection = new MagickImageCollection()){
                    for (int i = 0; i < 1; i++){

                        using (MagickImage MI_frame = new MagickImage()){
                            Byte[] frame = BMP_ToBytes(bmp_array[i]); 
                            MI_frame.Read(frame);
                            collection.Add(MI_frame);
                            collection[i].AnimationDelay = 33;
                            Console.WriteLine(frame[i]);
                        }
                    }
                    collection.Write(@"testoutput\test.gif");

                }
                       
            }
            
            Directory.CreateDirectory("output");
            new_img.Save(@"output\" + NewFileName);
            Console.WriteLine();

            Console.WriteLine("All done, image written to {0}", NewFileName);
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();

        }

        static Byte[] BMP_ToBytes(Bitmap bmp)
        {
            using (MemoryStream stream = new MemoryStream()){
                bmp.Save(stream, ImageFormat.Bmp);
                return stream.ToArray();
            }
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
            int res = Int32.Parse(dens);

            int r = gcd(width, height); int x = width / r; int y = height / r;

            //TODO: fix the aspect ratio for irregular sizes(placeholder for mona.jpg)
            if (x == width){
                Console.WriteLine();
                Console.WriteLine("Irregular image size.");
                return Tuple.Create(100 / res * 9 , 100 / res * 5);
            }

            return Tuple.Create(100 / res * x , 100 / res * y);
        }

        //slave function for calculate_aspect
        static int gcd(int a, int b)
        {
            if (b == 0) {
                return a;
            }
            else{
                return gcd(b, (a % b));
            }
        }

        //returns an array representing the image containing the voronoi sites
        static Point[,] point_generator(Tuple<int, int> ar, int xlen, int ylen)
        {
            int height_c = 0;
            Point[,] mat = new Point[xlen, ylen];
            Random rnd = new Random();

            //creates a random coordinate within the bounds of the array cell determined by the aspect ratio
            for (int y = 0; y < ylen; y++){
                int width_c = 0;
                for (int x = 0; x < xlen; x++){
                    int rnd_w = rnd.Next(width_c, width_c + ar.Item1);
                    int rnd_h = rnd.Next(height_c, height_c + ar.Item2);
                    Point vorsite = new Point(rnd_w, rnd_h);
                    mat[y, x] = vorsite;
                    width_c = ar.Item1 * x;
                }
                height_c = ar.Item2 * y;
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

    }
}
