using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FalconPoint4
{
    class Logistics
    {


        public double Heading(double lat1, double lon1,double lat2, double lon2)
        {
           
            double dLon = deg2rad(lon2 - lon1);
            double dPhi = Math.Log(Math.Tan(deg2rad(lat2) / 2 + Math.PI / 4) / Math.Tan(deg2rad(lat1) / 2 + Math.PI / 4));

            if (Math.Abs(dLon) > Math.PI)
            {
                dLon = dLon > 0 ? -(2 * Math.PI - dLon) : (2 * Math.PI + dLon);
            }

            return Math.Truncate(ToHeading(Math.Atan2(dLon, dPhi)));

        }

        public double SpeedMPH(double lat1, double lon1, double lat2, double lon2,DateTime time,DateTime time1)
        {
            TimeSpan ts = time - time1;
          //  MessageBox.Show(Distance(lat1, lon1, lat2, lon2).ToString() + " " + ts.Hours);
            return Math.Truncate((((((Distance(lat1, lon1, lat2, lon2) / (ts.Hours))/12)*60)*60)/5280)); //still don't know if this is right
        }

        public double ToHeading(double radians)
        {
            // convert radians to degrees (as heading: 0...360)
            return (rad2deg(radians) + 360) % 360;
        }

            public double Distance (double lat1, double lat2, double lon1, double lon2)
            {
               // double lat1 = 34.713496;
               // double lat2 = 34.713538;
               // double lon1 = -86.685663;
               // double lon2 = -86.688520;

                //double dlong = (lon2 - lon1) * 0.0174532925199433;
                //double dlat = (lat2 - lat1) * 0.0174532925199433;
                //double a = Math.Pow(Math.Sin(dlat / 2.0), 2) + Math.Cos(lat1 * 0.0174532925199433) * Math.Cos(lat2 * 0.0174532925199433) * Math.Pow(Math.Sin(dlong / 2.0), 2);
                //double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                //double d = 3956 * c; 


                double theta = lon1 - lon2;
                double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
                dist = Math.Acos(dist);
                dist = rad2deg(dist);
                dist = dist * 60 * 1.1515;
                dist = dist * .8684;

                return dist;
            }

            public double deg2rad(double deg)
            {
                return (deg * Math.PI / 180.0);
            }

            public double rad2deg(double rad)
            {
                return (rad / Math.PI * 180.0);
            }
    


    }
}
