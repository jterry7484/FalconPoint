using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Windows.Forms;
using fvw;
using System.Collections.Generic;

namespace FalconPoint4
{
    // Keeps track of UID and the layer that we put them on
    public class LayerList 
    {
        public string cotID;
        public int Layer;

        public IList<LatLongList> FP_LatLonlist = new List<LatLongList>(); // list to keep track of all lat and longs assoc. with a particular cotID

        public class LatLongList // used to keep track of all lat and longs assoc. with a particular cotID
        {
            public double lat;
            public double lon;
            public DateTime time;
        }
    }
    // Listens for feeds on a port.  If a feed is detected, then it starts the plotting process by calling FPdrawer
    class COTsListener
    {
        // Global variables and object init
        private TcpListener tcpListner;
        private Thread listenThread;
        public FPmain _FPmainAddr = null;

        public ILayer FP_point = null;
        int currentLayerHandle = 0;

        public IList<LayerList> FP_layerList = new List<LayerList>(); // used to keep track of UID and the layer that we put them on

        public LayerList temp_layerList = new LayerList(); // temp location for points before adding to FP_layerList

        // Starting component from FPmain.
        // Starts a new thread for port listening
        public COTsListener(FPmain _FPmain)
        {
            _FPmainAddr = _FPmain; // used to get address of of FPmain for use in creating layers... gotta have address of "this" to create a layer

            this.tcpListner = new TcpListener(IPAddress.Any, 3000);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        // On its own thread, listens for incoming feeds
        private void ListenForClients()
        {
            try
            {
                this.tcpListner.Start();

                while (true)
                {
                    TcpClient client = this.tcpListner.AcceptTcpClient();

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);
                }
            }
            catch { }
        }

        // Once a feed has been detected, this method deals with it
        private void HandleClientComm(object client)
        {
            string messageString = null;
            double currentLat;
            double currentLon;
            string currentID;
            DateTime currentTime;
            DateTime staleTime;

            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clienStream = tcpClient.GetStream();

            #region // some byte and ASCII encoding stuff to deal with our incoming message
            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = clienStream.Read(message, 0, 4096);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                {
                    break;
                }

                ASCIIEncoding encoder = new ASCIIEncoding();
            #endregion

                // Write out to the output window our incoming message
                System.Diagnostics.Debug.WriteLine(encoder.GetString(message, 0, bytesRead));

                // Sets our messageString variable to the new message
                messageString = encoder.GetString(message); 

                // Calls methods to that parse out information from the cots feed
                currentID = GetID(messageString);
                currentLat = GetLat(messageString);
                currentLon = GetLon(messageString);
                currentTime = GetTime(messageString);
                staleTime = GetStaleTime(messageString);

                AddCordsToList_andDraw(currentID, currentLat, currentLon, currentTime, staleTime);
            }

            tcpClient.Close();
        }

        /* Checks to see if the uid has a layer associated with it
         * If it does, then add this cot message to that uid's list of time, lat, lon
         * If it doesn't, then create a new item in the list
         * After items are added to list, then call FP_drawer
         */
        public void AddCordsToList_andDraw(string _cotID, double _lat, double _lon, DateTime time, DateTime staleTime)
        {
            //Haversine newDistance = new Haversine();

            //double distance = newDistance.Distance();

            bool cotID_alreadyExists = false;
            bool isDataStale = false;

            if (staleTime <= time)
                isDataStale = true;

            for(int i=0; i<FP_layerList.Count; i++) // itterate through list to see if the current cotID has already been added to list
            {
                if (FP_layerList[i].cotID == _cotID) // if we already have this cotID in our list
                {
                    FPdrawer drawPT_instance = new FPdrawer(); // create a new instance of the drawer class

                    drawPT_instance.CreatePoint(FP_point,FP_layerList[i].Layer, FP_layerList[i], _cotID, _lat, _lon, time, isDataStale); // if cot uid already exists in list, then use it's layer

                    LayerList.LatLongList temp_latLonList = new LayerList.LatLongList(); // temp list used to add lat and lon to our sub list... basically one main list holds LAYER and COTid and that list conists of another list that stores multiple lat, lon and times
                    temp_latLonList.lat = _lat;
                    temp_latLonList.lon = _lon;
                    temp_latLonList.time = time;

                    FP_layerList[i].FP_LatLonlist.Add(temp_latLonList); // add the temp data to the main list
                    temp_latLonList = new LayerList.LatLongList(); // create a new temp list

                    cotID_alreadyExists = true; // used to keep us from jumping into the next if... prob could use else
                }
            }

            if(cotID_alreadyExists == false) // if we don't have this cotID in our list
            {
                CreateLayer(_cotID); // if we don't have this cotID, then we need to create a new layer
                FPdrawer drawPT_instance = new FPdrawer(); // create a new instance of the drawer class
                drawPT_instance.CreatePoint(FP_point, currentLayerHandle,new LayerList(), _cotID, _lat, _lon, time, isDataStale); // use the newly created layer
            }
        }

        #region // Parsing functions that get lat, lon, id, time, etc. from our incoming cots messages

        public double GetLat(string _message)
        {
            string _rtnString = _message.TrimEnd('\0');
            int wheresLat = _rtnString.IndexOf("lat=") + 1;
            int whereFirstQuote = _rtnString.IndexOf("\"", wheresLat);
            int wheresLastQuote = _rtnString.IndexOf("\"", whereFirstQuote + 1);

            _rtnString = _rtnString.Remove(wheresLastQuote, (_rtnString.Length - wheresLastQuote)); // get rid of everything to the right of what we want

            _rtnString = _rtnString.Remove(0, whereFirstQuote + 1);

            return Convert.ToDouble(_rtnString);
        }

        public double GetLon(string _message)
        {
            string _rtnString = _message.TrimEnd('\0');
            int wheresLon = _rtnString.IndexOf("lon=") + 1;
            int whereFirstQuote = _rtnString.IndexOf("\"", wheresLon);
            int wheresLastQuote = _rtnString.IndexOf("\"", whereFirstQuote + 1);

            _rtnString = _rtnString.Remove(wheresLastQuote, (_rtnString.Length - wheresLastQuote)); // get rid of everything to the right of what we want

            _rtnString = _rtnString.Remove(0, whereFirstQuote + 1);

            return Convert.ToDouble(_rtnString);
        }

        public string GetID(string _message)
        {
            string _rtnString = _message.TrimEnd('\0');
            int wheresUID = _rtnString.IndexOf("uid=") + 1;
            int whereFirstQuote = _rtnString.IndexOf("\"", wheresUID);
            int wheresLastQuote = _rtnString.IndexOf("\"", whereFirstQuote + 1);

            _rtnString = _rtnString.Remove(wheresLastQuote, (_rtnString.Length - wheresLastQuote)); // get rid of everything to the right of what we want

            _rtnString = _rtnString.Remove(0, whereFirstQuote + 1);

            return _rtnString;
        }

        public DateTime GetTime(string _message)
        {
            // Data from feed looks like this -> time="2012-01-29T00:07:01Z"
            string _rtnString = _message.TrimEnd('\0');
            int wheresTime = _rtnString.IndexOf("time=") + 1;
            int whereFirstQuote = _rtnString.IndexOf("\"", wheresTime);
            int wheresLastQuote = _rtnString.IndexOf("\"", whereFirstQuote + 1);
            string _time = null;
            string _date = null;
            DateTime _rtnTime;

            _rtnString = _rtnString.Remove(wheresLastQuote, (_rtnString.Length - wheresLastQuote)); // get rid stuff to the right of field "time"
            _rtnString = _rtnString.TrimEnd('Z'); // trim the z off the end

            _rtnString = _rtnString.Remove(0, whereFirstQuote + 1); // get rid of stuff to the left of field "time"

            _date = _rtnString.Remove(_rtnString.IndexOf('T')); // adds everthing before the T to the _date string
            _time = _rtnString.Remove(0, _rtnString.IndexOf('T')+1); // adds everthing after the T to the _time string

            _rtnString = _date + " " + _time;
            _rtnTime = Convert.ToDateTime(_rtnString);

            return _rtnTime;
        }

        public DateTime GetStaleTime(string _message)
        {
            // Data from feed looks like this -> stale="2012-01-29T00:47:46Z"
            string _rtnString = _message.TrimEnd('\0');
            int wheresTime = _rtnString.IndexOf("stale=") + 1;
            int whereFirstQuote = _rtnString.IndexOf("\"", wheresTime);
            int wheresLastQuote = _rtnString.IndexOf("\"", whereFirstQuote + 1);
            string _time = null;
            string _date = null;
            DateTime _rtnTime;

            _rtnString = _rtnString.Remove(wheresLastQuote, (_rtnString.Length - wheresLastQuote)); // get rid stuff to the right of field "time"
            _rtnString = _rtnString.TrimEnd('Z'); // trim the z off the end

            _rtnString = _rtnString.Remove(0, whereFirstQuote + 1); // get rid of stuff to the left of field "time"

            _date = _rtnString.Remove(_rtnString.IndexOf('T')); // adds everthing before the T to the _date string
            _time = _rtnString.Remove(0, _rtnString.IndexOf('T') + 1); // adds everthing after the T to the _time string

            _rtnString = _date + " " + _time;
            _rtnTime = Convert.ToDateTime(_rtnString);

            return _rtnTime;
        }

        #endregion

        // Creates a new layer
        public int CreateLayer(string _cotID) // create a new layer, return the handle, and add info to list
        {
            int result = 50; // result is used for debugging... i used 50 so that i could tell that -1, 0, or 1 was the output... nothing special about 50
            FP_point = new LayerClass();

            result = FP_point.RegisterWithMapServer("falconpoint", 0, _FPmainAddr); // result is used for debugging
            currentLayerHandle = FP_point.CreateLayer("FP layer");

            System.Diagnostics.Debug.WriteLine("registered with map server result = " + currentLayerHandle); // used for debugging... shows registration result in output window

            temp_layerList.Layer = currentLayerHandle; // temp list item
            temp_layerList.cotID = _cotID; // temp list item

            FP_layerList.Add(temp_layerList); // add temp items to real list

            temp_layerList = new LayerList(); // create a new temp list... otherwise we would keep writing over the old list

            return currentLayerHandle; // return the newly created layer handle ... should be something like 102, 103..

        }
    }
}

