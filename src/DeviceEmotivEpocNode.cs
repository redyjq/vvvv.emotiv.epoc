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

namespace VVVV.EmotivEpoc
{
	#region PluginInfo
	[PluginInfo(Name = "EmotivEpoc",
				Category = "Device",
				Help = "Get information from Emotiv Epoc device",
				Tags = "Emotiv, Epoc, EEG",
				AutoEvaluate = true)]
	#endregion PluginInfo
	public class DeviceEmotivEpocNode : IPluginEvaluate, IDisposable, IPartImportsSatisfiedNotification
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
		
		[Output("Expressiv Value")]
		public ISpread<double> FExpressiv;
		
		[Output("Expressiv Legend")]
		public ISpread<string> FExpressivLegend;
		
//		[Output("Expressiv Upper Face")]
//		public ISpread<EdkDll.EE_ExpressivAlgo_t> FExpressivUpper;
//		
//		[Output("Expressiv Upper Face Power")]
//		public ISpread<double> FExpressivUpperPower;
//		
//		[Output("Expressiv Lower Face")]
//		public ISpread<EdkDll.EE_ExpressivAlgo_t> FExpressivLower;
//		
//		[Output("Expressiv Lower Face Poser")]
//		public ISpread<double> FExpressivLowerPower;
		
		[Output("Affectiv Value")]
		public ISpread<double> FAffectiv;
		
		[Output("Affectiv Legend")]
		public ISpread<string> FAffectivLegend;

		[Output("Connected", IsToggle = true, IsSingle = true)]
		public ISpread<bool> FConnected;

		[Import()]
		public ILogger FLogger;
		#endregion fields & pins
		
		private EmoEngine mEngine;
		private bool mIsConnected = false;
//		private ISpread<double>
		
		//Contructor
		public DeviceEmotivEpocNode() {
			mEngine = EmoEngine.Instance;
			
			//Register event handler
			mEngine.EmoStateUpdated += new EmoEngine.EmoStateUpdatedEventHandler(EmoStateUpdated);
		}
		
		
		//I/O pin that needs to be setup only once after constructor
		public void OnImportsSatisfied() {
			//Set the legends as they won't change dynamically
			ExpressivLegend();
			AffectivLegend();
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
			FExpressiv.SliceCount = 10;
			FAffectiv.SliceCount = 5;
			
			EmoState lES = e.emoState;
			float lEyeLidRight = 0;
			float lEyeLidLeft = 0;
			float lEyeX = 0;
			float lEyeY = 0;
			
			FExpressiv[0] = lES.ExpressivIsBlink() ? 1.0 : 0.0;
			FExpressiv[1] = lES.ExpressivIsRightWink() ? 1.0 : 0.0;
			FExpressiv[2] = lES.ExpressivIsLeftWink() ? 1.0 : 0.0;
			lES.ExpressivGetEyelidState(out lEyeLidLeft, out lEyeLidRight);
			FExpressiv[3] = lEyeLidRight;
			FExpressiv[4] = lEyeLidLeft;
			lES.ExpressivGetEyeLocation(out lEyeX, out lEyeY);
			FExpressiv[5] = lEyeX;
			FExpressiv[6] = lEyeY;
			FExpressiv[7] = lES.ExpressivGetEyebrowExtent();
			FExpressiv[8] = lES.ExpressivGetSmileExtent();
			FExpressiv[9] = lES.ExpressivGetClenchExtent();
			
			
//			FExpressivUpper[0] = lES.ExpressivGetUpperFaceAction();
//			FExpressivUpperPower[0] = lES.ExpressivGetUpperFaceActionPower();
//			
//			FExpressivLower[0] = lES.ExpressivGetLowerFaceAction();
//			FExpressivLowerPower[0] = lES.ExpressivGetLowerFaceActionPower();
			
			string str = "";
			FLogger.Log(LogType.Debug, "Event received:");
			foreach(double d in FExpressiv) {
				str += d;
			}
			FLogger.Log(LogType.Debug, str);
		}

		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
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
			
			FConnected.SliceCount = 1;
			FConnected[0] = mIsConnected;
		}
		
		
		//Expressiv legend values
		protected void ExpressivLegend() {
			FExpressivLegend.SliceCount = 10;
			FExpressivLegend[0] = "Blink";
			FExpressivLegend[1] = "Right Wink";
			FExpressivLegend[2] = "Left Wink";
			FExpressivLegend[3] = "Eyelid Right";
			FExpressivLegend[4] = "Eyelid Left";
			FExpressivLegend[5] = "Eyes Pos X";
			FExpressivLegend[6] = "Eyes Pos Y";
			FExpressivLegend[7] = "Eyebrow Extent";
			FExpressivLegend[8] = "Smile";
			FExpressivLegend[9] = "Clench";
		}
		
		
		//Affectiv legend values
		protected void AffectivLegend() {
			FAffectivLegend.SliceCount = 5;
			FAffectivLegend[0] = "Engagement/Boredom";
			FAffectivLegend[1] = "Frustration";
			FAffectivLegend[2] = "Meditation";
			FAffectivLegend[3] = "Instantaneous Excitement";
			FAffectivLegend[4] = "Long Term Excitement";
		}
	}
}
