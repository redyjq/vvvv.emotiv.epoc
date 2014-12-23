#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using Emotiv;

using VVVV.Core.Logging;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "EmotivEpoc",
				Category = "Device",
				Help = "Node to connect to the EmotivSDK and retrieve EmoStates",
				Tags = "",
				AutoEvaluate = true)]
	#endregion PluginInfo
	public class DeviceEmotivEpocNode : IPluginEvaluate, IDisposable
	{
		#region enums
		public enum TEmotivConnectionMode {
			EmoEngine,
			EmotivControlPanel,
			EmoComposer
		}
		#endregion enums
		
		
		#region fields & pins
		[Input("Connect", DefaultBoolean = false, IsToggle = true, IsSingle = true)]
		public IDiffSpread<bool> FConnect;
		
		[Input("Server", DefaultString = "127.0.0.1", IsSingle = true)]
		public IDiffSpread<string> FServer;
		
		[Input("Connection Mode", IsSingle = true, DefaultEnumEntry = "EmoEngine")]
		public IDiffSpread<TEmotivConnectionMode> ConnectionMode;

		[Output("Connected", IsToggle = true, IsSingle = true)]
		public ISpread<bool> FConnected;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		private EmoEngine mEngine;
		private bool mIsConnected = false;
		
		//Contructor
		public DeviceEmotivEpocNode() {
			mEngine = EmoEngine.Instance;
			
			//Register event handler
			mEngine.EmoStateUpdated += new EmoEngine.EmoStateUpdatedEventHandler(EmoStateUpdated);
		}
		
		
		//Destructor
		public void Dispose() {
			mEngine.Disconnect();
		}
		
		
		//Create the connection
		protected bool Connect(TEmotivConnectionMode iMode, string iServer) {
			switch(iMode) {
				case TEmotivConnectionMode.EmoEngine:
				mEngine.Connect();
				break;
				
				case TEmotivConnectionMode.EmotivControlPanel:
				mEngine.RemoteConnect(iServer, 3008);
				break;
				
				case TEmotivConnectionMode.EmoComposer:
				FLogger.Log(LogType.Debug, "Connecting...");
				mEngine.RemoteConnect(iServer, 1726);
				break;
			}
			
			FLogger.Log(LogType.Debug, "Connected!");
			return true;
		}
		
		
		//Handle data on EmoState update
		protected void EmoStateUpdated(object sender, EmoStateUpdatedEventArgs e) { 
			EmoState lES = e.emoState;
			
			Double rawScoreEc = 0, minScaleEc = 0, maxScaleEc = 0;
			lES.AffectivGetExcitementShortTermModelParams(out rawScoreEc, out minScaleEc, out maxScaleEc);
			FLogger.Log(LogType.Debug, "Excitement: " + lES.AffectivGetEngagementBoredomScore());
		}

		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FConnected.SliceCount = SpreadMax;
			if(FConnect.IsChanged) {
				if(FConnect[0] && !mIsConnected)
					mIsConnected = Connect(ConnectionMode[0], FServer[0]);
				else {
					mEngine.Disconnect();
					mIsConnected = false;
				}
			}
			
			if((FServer.IsChanged || ConnectionMode.IsChanged) && mIsConnected) {
				mEngine.Disconnect();
				mIsConnected = Connect(ConnectionMode[0], FServer[0]);
			}
			
			if(mIsConnected) {
				//Process events
				mEngine.ProcessEvents(1000);
			}
			
			FConnected[0] = mIsConnected;
		}
	}
}
