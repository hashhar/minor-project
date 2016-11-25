using System;
using System.IO;
using System.Net.Sockets;
using Newtonsoft.Json;

namespace WebApiClient
{
	partial class WebApiClient
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Starting client...");

			int port = 1234;
			TcpClient client = new TcpClient("localhost", port);
			NetworkStream stream = client.GetStream();
			StreamReader reader = new StreamReader(stream);
			StreamWriter writer = new StreamWriter(stream) { AutoFlush = true };

			while (true)
			{
				Console.Write("Enter to send: ");
				string lineToSend = Console.ReadLine();
				Console.WriteLine("Sending to server: " + lineToSend);
				writer.WriteLine(lineToSend);
				string lineReceived = reader.ReadLine();
				var jsonObj = JsonConvert.DeserializeObject<dataModel>(lineReceived);
				var jsonString = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
				Console.Write("\nReceived from server: " + jsonString + "\n\n");
			}
		}
	}
}
