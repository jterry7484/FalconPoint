using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fvw;
using System.Windows.Forms;
using System.Drawing;

namespace FalconPoint4
{
    class FPdrawer
    {
        string iconLoc = null;
        Logistics logistics = new Logistics();

        public FPdrawer()
        {

            if (System.Environment.Is64BitOperatingSystem == true)
                iconLoc = "C:\\Program Files (x86)\\PFPS\\falcon\\data\\icons\\Shape\\";
            else
                iconLoc = "C:\\Program Files\\PFPS\\falcon\\data\\icons\\Shape\\";
            
        }


        //Austen: changed input params to pass entire layer list so that we'd have access to all it's values
        public void CreatePoint(ILayer FP_point, int layer, LayerList layerList, string id, double lat, double lon, DateTime time, bool isStale)
        {
            if (isStale == false)
                iconLoc += "red turn.ico";
            else
                iconLoc += "white turn.ico";

            try
            {
                FP_point.DeleteAllObjects(layer);// delete everything on this layer.. keeps us from having bread crumbs 
              
                FP_point.AddIcon(layer, iconLoc, lat, lon, "");

                if (layerList.FP_LatLonlist.Count > 1)
                {
                    double lat1 = layerList.FP_LatLonlist[layerList.FP_LatLonlist.Count - 1].lat;
                    double lon1 = layerList.FP_LatLonlist[layerList.FP_LatLonlist.Count - 1].lon;
                    DateTime time1 = layerList.FP_LatLonlist[layerList.FP_LatLonlist.Count - 1].time;

                    double heading = logistics.Heading(lat, lon, lat1, lon1); //Compute heading
                    double speed = logistics.SpeedMPH(lat, lon, lat1, lon1, time, time1); //Compute Speed
                    RenderArrow(FP_point, layerList, lat, lon, time, heading);

                    AddUIDText(FP_point, layerList, lat, lon, heading, id);
                    AddHeadingText(FP_point, layerList, lat, lon, heading);
                    AddSpeedText(FP_point, layerList, lat, lon, speed, heading);
                }

                FP_point.Refresh(-1);
            }
            catch (Exception ex)
            {
                string error = ex.Message;
                MessageBox.Show("Error Message: "+ex.Message+ " Stack Trace: "+ex.StackTrace);
            }
        }

        public void RenderArrow(ILayer FP_point, LayerList layerList, double lat, double lon, DateTime time, double heading)
        {
      
            int symbolHandle = FP_point.CreateSymbol("arrow", 0);  //Create symbol 0 = doesn't already exist

            //add line to symbol from pixel 0,0 to 0,1 (use this to change where degree 0 is)
            FP_point.AddLineToSymbol(symbolHandle, 0, 0, 0, 2);
            FP_point.AddLineToSymbol(symbolHandle, 0, 2, 1, 1);
            FP_point.AddLineToSymbol(symbolHandle, 0, 2, -1, 1);

            //add created symbol to layer with lat long and rotation
            FP_point.AddSymbol(layerList.Layer, symbolHandle, lat, lon, 20, heading);                                                    

        }

        public void AddUIDText(ILayer FP_point, LayerList layerList, double lat, double lon, double heading, string id)
        {
            FP_point.AddText(layerList.Layer, lat, lon, id, OffsetText(heading), -70);
        }

        public void AddSpeedText(ILayer FP_point, LayerList layerList, double lat, double lon, double speed, double heading)
        {
            FP_point.AddText(layerList.Layer, lat, lon, speed + " MPH", OffsetText(heading), -55);
        }

        public void AddHeadingText(ILayer FP_point, LayerList layerList, double lat, double lon, double heading)
        {
            FP_point.AddText(layerList.Layer, lat, lon, heading + "°", OffsetText(heading), -40); // -13,-35 sets position of text
        }

        public int OffsetText(double heading)
        {
            if (heading > 140 && heading < 220)
                return -60;

            return -13;
        }

    }
}
