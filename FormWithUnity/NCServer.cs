using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using System;
using System.Net;
using NetworkCommsDotNet.DPSBase;
using NetworkCommsDotNet.Connections.TCP;
using System.Collections.Generic;
using UnitsNet;
using UnitsNet.Serialization.JsonNet;
using Newtonsoft.Json;
using Elements;
using UnitsNet.Units;

namespace FormWithUnity
{
    class NCServer
    {
        // for testing purpose
        private int position = -1;
        int index = 0;

        // for the communication 
        private bool serverEnabled = false;
        private int PortServer= 10000;
        private SendReceiveOptions customSendReceiveOptions;
        private List<IPEndPoint> connectedClients = new List<IPEndPoint>();
        

        // The delegate procedure we are assigning to our object
        public delegate void EventHandler(object myObject, Event myArgs);
        public event EventHandler OnGetSelected;

        /// <summary>
        /// constructor of the server
        /// </summary>
        public NCServer()
        {
            // setup the communication 
            customSendReceiveOptions = new SendReceiveOptions<ProtobufSerializer>();
            ConnectionInfo serverConnectionInfo = new ConnectionInfo(new IPEndPoint(IPAddress.Any, PortServer));

            //Start listening for incoming TCP connections
            if (!serverEnabled)
                EnableServer_Toggle();


            //Configure NetworkComms .Net to handle and incoming packet of type 'Communication'
            //e.g. If we receive a packet of type 'Communication' execute the method 'HandleIncomingCommunication'
            NetworkComms.AppendGlobalIncomingPacketHandler<Communication>("Communication", HandleIncomingCommunication, customSendReceiveOptions);

            // if a client disconnects
            NetworkComms.AppendGlobalConnectionCloseHandler(ClientDisconnected);
            
            // if a client connects
            NetworkComms.AppendGlobalConnectionEstablishHandler(ClientConnected);
            
            // if an update is received
            NetworkComms.AppendGlobalIncomingPacketHandler<CommunicateUpdate>("Update", (header, connection, message) =>
            {
                string content = message.Message;

                OnGetSelected(this, new Event(content)); // calls myNCServer.OnGetSelected += new NCServer.EventHandler(UpdateRichTextBox); in Form1
                //RichTextBox1.Text = content; // you can't update from this thread
            });

            // if we press that key, the server can receive it
            NetworkComms.AppendGlobalIncomingPacketHandler<int>("SecondaryIndexTrigger", (header, connection, message) =>
            {
                Console.WriteLine("SecondaryIndexTrigger");
                SendMyObject(); // here we ask for the creation of an object 
            });




        }
        

        /// <summary>
        /// client connected
        /// </summary>
        /// <param name="connection"></param>
        private void ClientConnected(Connection connection)
        {
            Console.WriteLine(" ");
            Console.WriteLine("Connection established: " + connection.ConnectionInfo.LocalEndPoint);
            
            lock (connectedClients)
            {
                //If a client sends a InitialClientConnect packet we add them to the connectedClients list
                connectedClients.Add((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);

                ICollection<IPEndPoint> withoutDuplicates = new HashSet<IPEndPoint>(connectedClients);
                connectedClients = new List<IPEndPoint>(withoutDuplicates);
            }

            

        }
        
        /// <summary>
         /// client disconnected
         /// </summary>
         /// <param name="connection"></param>
        private void ClientDisconnected(Connection connection)
        {
            Console.WriteLine(" ");
            Console.WriteLine("Connection is over: " + connection.ConnectionInfo.LocalEndPoint);
            
            lock (connectedClients)
            {
                connectedClients.Remove((IPEndPoint)connection.ConnectionInfo.RemoteEndPoint);
            }
        }





        /// <summary>
        /// Performs whatever functions we might so desire when we receive an incoming Communication
        /// </summary>
        /// <param name="header">The PacketHeader corresponding with the received object</param>
        /// <param name="connection">The Connection from which this object was received</param>
        /// <param name="incomingMessage">The incoming Communication we are after</param>
        private void HandleIncomingCommunication(PacketHeader header, Connection connection, Communication incomingMessage)
        {
            if (NetworkComms.NetworkIdentifier!=connection.ConnectionInfo.NetworkIdentifier)
            {
                position = incomingMessage.Message;
                Console.WriteLine("Message received from: " + connection.ConnectionInfo.LocalEndPoint + ", position updated: " + incomingMessage.Message );
                // for example purpose 
            }
        }

        /// <summary>
        /// we can send messages
        /// </summary>
        public void SendMessage() {

            Communication message = new Communication(NetworkComms.NetworkIdentifier, position);

            //Print out the IPs and ports we are now listening on
            Console.WriteLine();
            Console.WriteLine("[Communication] Server messaging these TCP connections:");

            foreach (IPEndPoint localEndPoint in connectedClients)
            {
                Console.WriteLine($"{localEndPoint.Address}:{localEndPoint.Port}");
                try
                {
                    TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Communication", message);

                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }

            Console.WriteLine("Server stops messaging");
            Console.WriteLine();
            

        }

        /// <summary>
        /// you can also send objects, here we create a xxx, i mean we send a xxx by tcp and then the client creates it after deserializing and other stuff
        /// </summary>
        public void SendMyObject()
        {
            ElbowCylindrical elbowCylindrical = new ElbowCylindrical(1, "ElbowCylindrical", "Element", 1, 1, new Length(0.01, LengthUnit.Meter), new Length(0.1, LengthUnit.Meter), new Angle(90, AngleUnit.Degree), new Length(0, LengthUnit.Meter), false, new Length(0, LengthUnit.Meter), new Length(0.4, LengthUnit.Meter), new Length(0, LengthUnit.Meter), new Angle(0, AngleUnit.Degree), new Angle(0, AngleUnit.Degree));
            Caps caps = new Caps(1, "Caps", "Element", 1, 1, "Caps", new Length(0.2, LengthUnit.Meter), new Length(0.5, LengthUnit.Meter), new Length(0.4999, LengthUnit.Meter), new Length(0.5, LengthUnit.Meter), new Length(1, LengthUnit.Meter), new Length(0, LengthUnit.Meter), new Pressure(0, PressureUnit.Bar), "Caps", false);
            PipeRectangular pipeRectangular = new PipeRectangular(1, "PipeRectangular", "Element", 1, 1, "PipeRectangular", "Shape", new Length(0.1, LengthUnit.Meter), new Length(1, LengthUnit.Meter), new Length(0.5, LengthUnit.Meter), new Length(0.5, LengthUnit.Meter));
            Cone cone = new Cone(1, "Tube", "Element", 1, 1, new Length(0.1, LengthUnit.Meter), new Length(2, LengthUnit.Meter), new Length(1, LengthUnit.Meter), new Length(2, LengthUnit.Meter), new Angle(0, AngleUnit.Degree), new Length(0,LengthUnit.Meter), new Length(0, LengthUnit.Meter), new Length(0, LengthUnit.Meter), new Length(0, LengthUnit.Meter));
            Tube tube = new Tube(1, "Tube", "Element", 1, 1, "tube", new Length(1, LengthUnit.Meter), new Length(0.1, LengthUnit.Meter), new Length(2, LengthUnit.Meter));
            List<BaseElement> objectsList = new List<BaseElement> { elbowCylindrical, caps, pipeRectangular, cone, tube};
            index = index >= objectsList.Count - 1 ? index = 0 : index + 1;
            BaseElement c = objectsList[index];

            JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            };
            _jsonSerializerSettings.Converters.Add(new UnitsNetJsonConverter());
            string json = JsonConvert.SerializeObject(c, typeof(BaseElement), _jsonSerializerSettings);

            CommunicateElement messageObject = new CommunicateElement(NetworkComms.NetworkIdentifier, json, c.Designation);
            
            //Print out the IPs and ports we are now listening on
            Console.WriteLine();
            Console.WriteLine("[Object] Server messaging these TCP connections:");

            foreach (IPEndPoint localEndPoint in connectedClients)
            {
                Console.WriteLine($"{localEndPoint.Address}:{localEndPoint.Port}");
                try
                {
                    TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Element", messageObject);
                   
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }

            Console.WriteLine("Server stops messaging");
            Console.WriteLine();
        }

        /// <summary>
        /// get the string between the two given strings
        /// </summary>
        /// <param name="strSource">source you are interested in extract something</param>
        /// <param name="strStart">first string</param>
        /// <param name="strEnd">second string</param>
        /// <returns></returns>
        private static string GetBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        } // credit https://stackoverflow.com/questions/10709821/find-text-in-string-with-c-sharp

        /// <summary>
        /// if we update an item in the interface, it tells the client and it updates
        /// </summary>
        /// <param name="text"></param>
        public void SendObjectUpdate(string text)
        {
            
            if (text.Length > 0)
            {
                // transform the text in a valid format ?
                string content = text;

                // get the ID
                string stringID = GetBetween(content, "<ID", "ID>");
                if (!Int32.TryParse(stringID, out int ID))
                {
                    ID = -1;
                }
                
                // transform text into CommunicateUpdate element
                CommunicateUpdate messageUpdate = new CommunicateUpdate(NetworkComms.NetworkIdentifier, content, ID);


                //Print out the IPs and ports we are now listening on
                Console.WriteLine();
                Console.WriteLine("[Update] Server messaging these TCP connections:");

                foreach (IPEndPoint localEndPoint in connectedClients)
                {
                    Console.WriteLine($"{localEndPoint.Address}:{localEndPoint.Port}" );
                    try
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Update", messageUpdate);

                    }
                    catch (CommunicationException _cex) { Console.WriteLine("CommunicationException" + _cex); }
                    catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                    catch (Exception) { Console.WriteLine("Autre exception"); }
                }

                Console.WriteLine("Server stops messaging");
                Console.WriteLine();
            }
        }


        /// <summary>
        /// Toggle whether the local application is acting as a server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableServer_Toggle()
        {
            //Enable or disable the local server mode depending on if the server is already enabled or not
            ToggleServerMode(!serverEnabled);
            serverEnabled = !serverEnabled;
            
        }

        /// Wrap the functionality required to enable/disable the local application server mode
        /// </summary>
        /// <param name="enableServer"></param>
        private void ToggleServerMode(bool enableServer)
        {
            if (enableServer)
            {
                //Start listening for new incoming TCP connections
                //Parameters ensure we listen across all adaptors using a random port
                TCPConnection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, PortServer));
            }
            
            else
            {
                ShutDown();
            }
        }

      
        /// <summary>
        /// closes the communication
        /// </summary>
        public void ShutDown() {
            //We have used NetworkComms so we should ensure that we correctly call shutdown
            NetworkComms.Shutdown();
        }


        /// <summary>
        /// for testing purpose
        /// </summary>
        /// <returns></returns>
        public int GetPosition() { return position; }

        /*
        // what goes next is more for example purpose, the client has no handler with that type of message
        /// <summary>
        /// makes u go right
        /// </summary>
        /// <param name="val"></param>
        public void Right(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "right");
                       
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }
        
        /// <summary>
        /// makes u go bot
        /// </summary>
        /// <param name="val"></param>
        public void Bot(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "bot");
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }

        /// <summary>
        /// makes u go left
        /// </summary>
        /// <param name="val"></param>
        public void Left(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "left");
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }

        /// <summary>
        /// makes u go top
        /// </summary>
        /// <param name="val"></param>
        public void Top(int val)
        {
            foreach (System.Net.IPEndPoint localEndPoint in connectedClients)
            {
                try
                {
                    for (int i = 0; i < val; i++)
                    {
                        TCPConnection.GetConnection(new ConnectionInfo(localEndPoint), customSendReceiveOptions).SendObject("Deplacement", "top");
                    }
                }
                catch (CommunicationException) { Console.WriteLine("CommunicationException"); }
                catch (ConnectionShutdownException) { Console.WriteLine("ConnectionShutdownException"); }
                catch (Exception) { Console.WriteLine("Autre exception"); }
            }
        }
        //*/
    }


    
}
