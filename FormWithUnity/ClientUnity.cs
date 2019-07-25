using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class Client : MonoBehaviour
{


    public string ipAddress = "127.0.0.1";
    public int port = 54010;
    private TcpClient m_client;
    private string m_receivedMessage = "";
    private int position = 0;
    private NetworkStream m_netStream = null;
    private byte[] m_buffer = new byte[49152];
    private int m_bytesReceived = 0;



    /*
    public Client() {

        Debug.Log("coucou");
    }*/

    // Start is called before the first frame update
    void Start()
    {

        //Early out
        if (m_client != null)
        {
            return;
        }

        try
        {
            //Create new client
            m_client = new TcpClient();
            //Set and enable client
            m_client.Connect(ipAddress, port);
            // the client is correcly initialized if there is a server open at the ipAddress and the port
            // otherwise full bugs

        }
        catch (SocketException)
        {
            CloseClient();
        }
    }

    //Close client connection
    private void CloseClient()
    {
        if (m_client.Connected)
        {
            //Reset everything to defaults
            m_client.Close();
            m_client = null;
        }
    }



    // Update is called once per frame
    void Update()
    {
        //Debug.Log(m_buffer.ToString() + " you can see that the buffer is correctly initialized");
        if (m_client.Connected)
        {
            m_netStream = m_client.GetStream();
            //Debug.Log(m_netStream.ToString() + " you can see that the netStream is correctly initialized");
            if (m_netStream.CanRead)
            {
                m_netStream.BeginRead(m_buffer, 0, m_buffer.Length, new AsyncCallback(MessageReceived), null);
                //Debug.Log(m_receivedMessage + " you can see that the message is correctly initialized");
                // it's also not working if the server is not open

                //Debug.Log(m_receivedMessage);
            }


            //If there is something received
            if (!string.IsNullOrEmpty(m_receivedMessage))
            {

                //ClientLog("Msg recived on Client: " + "<b>" + m_receivedMessage + "</b>", Color.green);

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.position = new Vector3(1.0f + position, 0, 0);
                position++;
                m_receivedMessage = "";
            }

            //Build message to server
            string sendMsg = position.ToString();
            byte[] msg = Encoding.ASCII.GetBytes(sendMsg);
            //Start Sync Writing
            m_netStream.Write(msg, 0, msg.Length);
        }
    }

    //Callback called when "BeginRead" is ended
    private void MessageReceived(IAsyncResult result)
    {

        if (result.IsCompleted && m_client.Connected)
        {
            //build message received from server
            m_bytesReceived = m_netStream.EndRead(result);
            m_receivedMessage = Encoding.ASCII.GetString(m_buffer, 0, m_bytesReceived);

            //If message recived from server is "Close", close that client
            if (m_receivedMessage == "Close")
            {

                m_netStream = m_client.GetStream();
                string sendMsg = "Close";
                byte[] msg = Encoding.ASCII.GetBytes(sendMsg);
                //Start Sync Writing
                m_netStream.Write(msg, 0, msg.Length);
                CloseClient();
            }
        }
    }


    public int GetPosition() { return position; }

}
