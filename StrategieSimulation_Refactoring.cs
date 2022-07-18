using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using AgenaTrader.API;
using AgenaTrader.Custom;
using AgenaTrader.Plugins;
using AgenaTrader.Helper;

namespace AgenaTrader.UserCode
{
	[TimeFrameRequirements("1 Week HA", "4 Hour HA", "15 Min HA")]
	public class HeikinAshiSystem : UserIndicator
	{
		//----------------------------------------------------------------------
		// Timeframes
		//
		TimeFrame TF_Week = new TimeFrame("1 Week HA");
		TimeFrame TF_4Hour = new TimeFrame("4 Hour HA");
		TimeFrame TF_15Min = new TimeFrame("15 Min HA");

		

		//----------------------------------------------------------------------
		// Datenserien
		//
		public DataSeries LastSwingHigh;
		public DataSeries LastSwingLow;
		public DataSeries PreviousLastSwingHigh;
		public DataSeries PreviousLastSwingLow;

		public BoolSeries UpTrend;
		public BoolSeries DownTrend;
		public BoolSeries NoTrend;
		public StringSeries PreviousTrend;

		public BoolSeries UpTrendSignal;
		public BoolSeries DownTrendSignal;

		
		//----------------------------------------------------------------------
		// Agena Script Ereignisse
		//
		protected override void OnInit()
		{
			IsOverlay = true;

			LastSwingHigh = new DataSeries(this);
			LastSwingLow = new DataSeries(this);
			PreviousLastSwingHigh = new DataSeries(this);
			PreviousLastSwingLow = new DataSeries(this);

			UpTrend = new BoolSeries(this);
			DownTrend = new BoolSeries(this);
			NoTrend = new BoolSeries(this);
			PreviousTrend = new StringSeries(this);

			UpTrendSignal = new BoolSeries(this);
			DownTrendSignal = new BoolSeries(this);

			AddOutput(new OutputDescriptor(new Pen(Color.FromKnownColor(KnownColor.Green), 1), "IndicatorLastSwingHigh"));
			AddOutput(new OutputDescriptor(new Pen(Color.FromKnownColor(KnownColor.Red), 1), "IndicatorLastSwingLow"));
		}

       	protected override void OnBarsRequirements()
		{
			base.OnBarsRequirements();
			Add(TF_Week);
			Add(TF_4Hour);
			Add(TF_15Min);
		}

		protected override void OnCalculate()
		{
			
			var itemWeek = MultiBars.GetBarsItem(TF_Week);
			var item4Hour = MultiBars.GetBarsItem(TF_4Hour);
			var item15Min = MultiBars.GetBarsItem(TF_15Min);

								
			Moving moving = new Moving(Open, Close, High, Low, LastSwingHigh, LastSwingLow, PreviousLastSwingHigh, PreviousLastSwingLow);
			Breakout breakout = new Breakout(Open, Close, High, Low, LastSwingHigh, LastSwingLow);
			Trend trend = new Trend(Open, Close, High, Low, moving, breakout, UpTrend, DownTrend, NoTrend, PreviousTrend);
			

			testing(moving, breakout, trend);
		}

		

		//----------------------------------------------------------------------
		// Controller
		//
		private void controller(Moving moveBigtrend)
        {
			
		}



		//----------------------------------------------------------------------
		// Testing
		//
		private void testing(Moving moving, Breakout breakout, Trend trend)
							 
        {
			moving.Run();
			breakout.Run();
			trend.Run();

			testing_outputs_moving(moving);
			testing_outputs_breakout(breakout);
			testing_output_trend(trend);
		}



		//----------------------------------------------------------------------
		// Testing Outputs
		//
		private void testing_outputs_moving(Moving moving)
        {
			bool output_MovingUp = false;
			bool output_MovingDown = false;
			bool output_MovingChangeUp = false;
			bool output_MovingChangeDown = false;
			bool output_LastSwingHigh = false;
			bool output_LastSwingLow = false;
			bool output_PreviousLastSwingHigh = false;
			bool output_PreviousLastSwingLow = false;
			bool output_IndicatorLastSwingHigh = true;
			bool output_IndicatorLastSwingLow = true;


			if (moving.MovingUp && output_MovingUp)
            {
				AddChartText("Text1" + ProcessingBarIndex, moving.MovingUp.ToString(), 0, High[0], Color.Black);
			}

			if (moving.MovingDown && output_MovingDown)
			{
				AddChartText("Text2" + ProcessingBarIndex, moving.MovingDown.ToString(), 0, High[0], Color.Black);
			}

			if (moving.MovingChangeUp && output_MovingChangeUp)
			{
				BackColorAll = Color.FromArgb(50, Color.Green);
			}

			if (moving.MovingChangeDown && output_MovingChangeDown)
			{
				BackColorAll = Color.FromArgb(50, Color.Red);
			}

			if (output_LastSwingHigh)
            {
				AddChartText("Text3" + ProcessingBarIndex, moving.LastSwingHigh[0].ToString(), 0, High[0], Color.Black);
			}

			if (output_LastSwingLow)
            {
				AddChartText("Text4" + ProcessingBarIndex, moving.LastSwingLow[0].ToString(), 0, Low[0], Color.Black);
			}

			if (output_PreviousLastSwingHigh)
            {
				AddChartText("Text5" + ProcessingBarIndex, moving.PreviousLastSwingHigh[0].ToString(), 0, High[0], Color.Black);
			}

			if (output_PreviousLastSwingLow)
            {
				AddChartText("Text6" + ProcessingBarIndex, moving.PreviousLastSwingLow[0].ToString(), 0, Low[0], Color.Black);
			}

			if (output_IndicatorLastSwingHigh)
			{
				IndicatorLastSwingHigh.Set(moving.LastSwingHigh[0]);
			}

			if (output_IndicatorLastSwingLow)
			{
				IndicatorLastSwingLow.Set(moving.LastSwingLow[0]);
			}

		}


		private void testing_outputs_breakout(Breakout breakout)
        {
			bool output_BreakoutUp = false;
			bool output_BreakoutDown = false;

			if (breakout.BreakoutUp && output_BreakoutUp)
            {
				BackColorAll = Color.FromArgb(50, Color.Green);
			}

			if (breakout.BreakoutDown && output_BreakoutDown)
            {
				BackColorAll = Color.FromArgb(50, Color.Red);
			}
        }


		private void testing_output_trend(Trend trend)
        {
			bool output_trendUp = true;
			bool output_trendDown = true;
			bool output_noTrend = false;
			bool output_previousTrend = false;

			if (trend.UpTrend[0] && output_trendUp)
            {
				BackColorAll = Color.FromArgb(50, Color.Green);
			}

			if (trend.DownTrend[0] && output_trendDown)
            {
				BackColorAll = Color.FromArgb(50, Color.Red);
			}

			if (trend.NoTrend[0] && output_noTrend)
            {
				BackColorAll = Color.FromArgb(50, Color.Blue);
			}

			if (output_previousTrend)
            {
				AddChartText("Text8" + ProcessingBarIndex, trend.PreviousTrend[0].ToString(), 0, High[0], Color.Black);
			}
        }
		

		

        //----------------------------------------------------------------------
        // Datenserien (Indikatoren)
        //
        #region
        [Browsable(false)]
		[XmlIgnore()]
		public DataSeries IndicatorLastSwingHigh
		{
			get { return Outputs[0]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public DataSeries IndicatorLastSwingLow
		{
			get { return Outputs[1]; }
		}

        #endregion

    }









	// **************************************************************************
	// Klasse Moving
	// **************************************************************************
	public class Moving : UserIndicator
	{
		// Properties
		private new IDataSeries Open { get; }
		private new IDataSeries Close { get; }
		private new IDataSeries High { get; }
		private new IDataSeries Low { get; }

		public DataSeries LastSwingHigh { get; private set; }
		public DataSeries LastSwingLow { get; private set; }
		public DataSeries PreviousLastSwingHigh { get; private set; }
		public DataSeries PreviousLastSwingLow { get; private set; }

		public bool MovingUp { get; private set; }
		public bool MovingDown { get; private set; }
		public bool MovingChangeUp { get; private set; }
		public bool MovingChangeDown { get; private set; }
		

		// Konstruktor
		public Moving(IDataSeries Open, IDataSeries Close, IDataSeries High, IDataSeries Low,
					  DataSeries LastSwingHigh, DataSeries LastSwingLow, 
					  DataSeries PreviousLastSwingHigh, DataSeries PreviousLastSwingLow)
					  
		{
			this.Open = Open;
			this.Close = Close;
			this.High = High;
			this.Low = Low;
			this.LastSwingHigh = LastSwingHigh;
			this.LastSwingLow = LastSwingLow;
			this.PreviousLastSwingHigh = PreviousLastSwingHigh;
			this.PreviousLastSwingLow = PreviousLastSwingLow;
		}


		//----------------------------------------------------------------------
		// Ausfuehren der Berechnungen 
		//
		public void Run()
        {
			CalculateMovingDirection();
			CalculateMovingChange();
			CalculateLastSwingPoint();
		}


		//----------------------------------------------------------------------
		// Berechnet die Bewegungsrichtung (gruene oder rote Kerze)
		//
		public void CalculateMovingDirection()
		{
			bool isMovingUp = Close[0] > Open[0];
			bool isMovingDown = Close[0] < Open[0];
			
			if (isMovingUp)
			{
				MovingUp = true;
				MovingDown = false;
			}

			if (isMovingDown)
			{
				MovingUp = false;
				MovingDown = true;
			}
		}


		//----------------------------------------------------------------------
		// Berechnet ob es eine Aenderung des Bewegungsrichtung gibt
		//
		public void CalculateMovingChange()
		{
			bool isChangingUp = Close[0] > Open[0] && Close[1] < Open[1];
			bool isChangingDown = Close[0] < Open[0] && Close[1] > Open[1];
			bool isChangingUpAndDojiBefore = Close[0] > Open[0] && Close[1] == Open[1] && Close[2] < Open[2];
			bool isChangingDownaAndDojiBefore = Close[0] < Open[0] && Open[1] == Close[1] && Close[2] < Open[2];

			if (isChangingUp || isChangingUpAndDojiBefore)
			{
				MovingChangeUp = true;
			}
			else if (isChangingDown || isChangingDownaAndDojiBefore)
			{
				MovingChangeDown = true;
			}
			else
			{
				MovingChangeUp = false;
				MovingChangeDown = false;
			}
		}


		//----------------------------------------------------------------------
		// Berechnet nach einem Farbwechsel die letzten SwingPoints, PreviousSwingPoints
		//
		public void CalculateLastSwingPoint()
        {
			// Farbwechsel nach oben (gruen)
			if (MovingChangeUp)
            {
				for (int i=0; i<=10; i++)
                {
					bool compareLows = Low[i] < Low[i+1];
					if (compareLows)
					{
						LastSwingLow.Set(Low[i]);
						PreviousLastSwingLow.Set(LastSwingLow[1]);
						break;
					}
				}
			}
			else
			{
				LastSwingLow.Set(LastSwingLow[1]);
				PreviousLastSwingLow.Set(PreviousLastSwingLow[1]);
			}
					

			// Farbwechsel nach unten (gruen)
			if (MovingChangeDown)
            {
				for (int i=0; i<=10; i++)
				{
					bool compareHighs = High[i] > High[i + 1];
					if (compareHighs)
					{
						LastSwingHigh.Set(High[i]);
						PreviousLastSwingHigh.Set(LastSwingHigh[1]);
						break;
					}
				}	
			}
			else
            {
				LastSwingHigh.Set(LastSwingHigh[1]);
				PreviousLastSwingHigh.Set(PreviousLastSwingHigh[1]);
            }

			
		}

				
	}





	// **************************************************************************
	// Klasse Breakout
	// **************************************************************************
	public class Breakout : UserIndicator
    {
		// Properties
		private new IDataSeries Open { get; }
		private new IDataSeries Close { get; }
		private new IDataSeries High { get; }
		private new IDataSeries Low { get; }

		private DataSeries LastSwingHigh { get; }
		private DataSeries LastSwingLow { get; }
		
		public bool BreakoutUp { get; private set; }
		public bool BreakoutDown { get; private set; }
				

		// Konstruktor
		public Breakout(IDataSeries Open, IDataSeries Close, IDataSeries High, IDataSeries Low,
						DataSeries LastSwingHigh, DataSeries LastSwingLow)
        {
			this.Open = Open;
			this.Close = Close;
			this.High = High;
			this.Low = Low;
			this.LastSwingHigh = LastSwingHigh;
			this.LastSwingLow = LastSwingLow;
			
		}


		//----------------------------------------------------------------------
		// Ausfuehren der Berechnungen
		//
		public void Run()
        {
			CalculateBreakoutUp();
			CalculateBreakoutDown();
        }


		//----------------------------------------------------------------------
		// Berechnet ob es einen Durchburch nach oben gab (Breakout Down)
		//
		public void CalculateBreakoutUp()
        {
			//bool priceCrossAboveLastSwingHigh = item.High[0] > LastSwingHigh[0] && item.High[1] <= LastSwingHigh[1];		// Breakout High
			bool priceCrossAboveLastSwingHigh = Close[0] > LastSwingHigh[0] && Close[1] <= LastSwingHigh[1];		// Breakout Close
			bool lastSwingUpHasValue = LastSwingHigh[0] != 0;

			if (priceCrossAboveLastSwingHigh && lastSwingUpHasValue)
			{
				BreakoutUp = true;
			}
		}


		//----------------------------------------------------------------------
		// Berechnet ob es einen Durchburch nach unten gab (Breakout Down)
		//
		public void CalculateBreakoutDown()
        {
			//bool priceCrossBelowLastSwingLow = item.Low[0] < LastSwingLow[0] && item.Low[1] >= LastSwingLow[1];         // Breakout Low
			bool priceCrossBelowLastSwingLow = Close[0] < LastSwingLow[0] && Close[1] >= LastSwingLow[1];		  // Breakout Close
			bool lastSwingDownHasValue = LastSwingLow[0] != 0;

			if (priceCrossBelowLastSwingLow && lastSwingDownHasValue)
            {
				BreakoutDown = true;
            }
        }

    }





	// **************************************************************************
	// Klasse Trend
	// **************************************************************************
	public class Trend : UserIndicator
    {
		// Properties
		private new IDataSeries Open { get; }
		private new IDataSeries Close { get; }
		private new IDataSeries High { get; }
		private new IDataSeries Low { get; }
		private Moving moving { get; }
		private Breakout breakout{ get; }
		
		
		public BoolSeries UpTrend { get; private set; }
		public BoolSeries DownTrend { get; private set; }
		public BoolSeries NoTrend { get; private set; }
		public StringSeries PreviousTrend { get; private set; }

        
		// Konstruktor
        public Trend(IDataSeries Open, IDataSeries Close, IDataSeries High, IDataSeries Low,
					 Moving moving,
					 Breakout breakout, 
					 BoolSeries UpTrend, BoolSeries DownTrend, BoolSeries NoTrend,
					 StringSeries PreviousTrend)
    	{
			this.Open = Open;
			this.Close = Close;
			this.High = High;
			this.Low = Low;
			this.moving = moving;
			this.breakout = breakout;
			this.UpTrend = UpTrend;
			this.DownTrend = DownTrend;
			this.NoTrend = NoTrend;
			this.PreviousTrend = PreviousTrend;
		}


		//----------------------------------------------------------------------
		// Ausfuehren der Berechnungen
		//
		public void Run()
        {
			CalculateCurrentTrend();
			CalculateTrendBreak();
			CalculateResumeTrend();
		}


		//----------------------------------------------------------------------				//TODO: Signale in eigene Klasse auslagern
		// Berechnet ob ein Aufwaertstrend vorliegt (2 hoehere Hochs, 2 hoehere Tiefs)
		// Ausgabe als Signal
		//
		public void CalculateUpTrendSignal()
        {
			bool breakoutUp = breakout.BreakoutUp;
			bool higherLows = moving.LastSwingLow[0] > moving.PreviousLastSwingLow[0];

			if (breakoutUp && higherLows)
            {
				UpTrend.Set(true);
				DownTrend.Set(false);
				NoTrend.Set(false);
			}
		}


		//----------------------------------------------------------------------				//TODO: Signale in eigene Klasse auslagern
		// Berechnet ob ein Abwaertstrend vorliegt (2 tiefere Tiefs, 2 tiefere Hochs)
		// Ausgabe als Signal
		//
		public void CalculateDownTrendSignal()
        {
			bool breakoutDown = breakout.BreakoutDown;
			bool lowerHighs = moving.LastSwingHigh[0] < moving.PreviousLastSwingHigh[0];

			if (breakoutDown && lowerHighs)
            {
				UpTrend.Set(false);
				DownTrend.Set(true);
				NoTrend.Set(false);
            }
		}


		//----------------------------------------------------------------------
		// Berechnet den derzeit vorhandenen Trend
		//
		public void CalculateCurrentTrend()
        {
			bool breakoutUp = breakout.BreakoutUp;
			bool higherLows = moving.LastSwingLow[0] > moving.PreviousLastSwingLow[0];

			bool breakoutDown = breakout.BreakoutDown;
			bool lowerHighs = moving.LastSwingHigh[0] < moving.PreviousLastSwingHigh[0];

			if (breakoutUp && higherLows)
			{
				UpTrend.Set(true);
				DownTrend.Set(false);
				NoTrend.Set(false);
			}

			else if (breakoutDown && lowerHighs)
			{
				UpTrend.Set(false);
				DownTrend.Set(true);
				NoTrend.Set(false);
			}

			else 
            {
				UpTrend.Set(UpTrend[1]);
				DownTrend.Set(DownTrend[1]);
				NoTrend.Set(NoTrend[1]);
				PreviousTrend.Set(PreviousTrend[1]);
            }

		}

		

		//----------------------------------------------------------------------
		// Berechnet ob ein Trendbruch auftritt
		//
		public void CalculateTrendBreak() 
        {
			//bool trendBreakUpTrend = item.Low[0] < moving.LastSwingLow[0] && item.Low[1] >= moving.LastSwingLow[1];         // Trendbruch Low	
			//bool trendBreakUpTrend = item.Close[0] < moving.LastSwingLow[0] && item.Close[1] >= moving.LastSwingLow[1];		  // Trendbruch Close
			bool trendBreakUpTrend = breakout.BreakoutDown;
			bool upTrend = UpTrend[0];

			//bool trendBreakDownTrend = item.High[0] > moving.LastSwingHigh[0] && item.High[1] <= moving.LastSwingHigh[1];		// Trendbruch High
			//bool trendBreakDownTrend = item.Close[0] > moving.LastSwingHigh[0] && item.Close[1] <= moving.LastSwingHigh[1];		// Trendbruch Close
			bool trendBreakDownTrend = breakout.BreakoutUp;
			bool downTrend = DownTrend[0];

			if (upTrend && trendBreakUpTrend)
            {
				UpTrend.Set(false);
				DownTrend.Set(false);
				NoTrend.Set(true);
				PreviousTrend.Set("UpTrend");
            }

			else if (downTrend && trendBreakDownTrend)
            {
				UpTrend.Set(false);
				DownTrend.Set(false);
				NoTrend.Set(true);
				PreviousTrend.Set("DownTrend");
            }
					
        }


		//----------------------------------------------------------------------
		// Berechnet die Wiederaufnahme eines zuvor gebrochenen Trends
		//
		public void CalculateResumeTrend()
        {
			bool noTrend = NoTrend[0];
			bool previousTrendisUp = PreviousTrend[0] == "UpTrend";
			bool previousTrendisDown = PreviousTrend[0] == "DownTrend";
			bool resumeUpTrend = Close[0] > moving.LastSwingHigh[0] && Close[1] <= moving.LastSwingHigh[1];
			bool resumeDownTrend = Close[0] < moving.LastSwingLow[0] && Close[1] >= moving.LastSwingLow[1];
			bool resumeUpTrendAfterCounterTrendDown = Close[0] > moving.PreviousLastSwingHigh[0] && Close[1] <= moving.PreviousLastSwingHigh[1];
			bool resumeDownTrendAfterCounterTrendUp = Close[0] < moving.PreviousLastSwingLow[0] && Close[1] >= moving.PreviousLastSwingLow[1];
			bool LastSwingHighLowerThanPreviousLastSwingHigh = moving.LastSwingHigh[0] < moving.PreviousLastSwingHigh[0];
			bool LastSwingLowHigerThanPreviousLastSwingLow = moving.LastSwingLow[0] > moving.PreviousLastSwingHigh[0];

			if (noTrend && previousTrendisUp && resumeUpTrend)
            {
				UpTrend.Set(true);
				DownTrend.Set(false);
				NoTrend.Set(false);
            }

			else if (noTrend && previousTrendisDown && resumeDownTrend)
            {
				UpTrend.Set(false);
				DownTrend.Set(true);
				NoTrend.Set(false);
            }

			else if (noTrend && previousTrendisDown && resumeUpTrendAfterCounterTrendDown && LastSwingHighLowerThanPreviousLastSwingHigh)
            {
				UpTrend.Set(true);
				DownTrend.Set(false);
				NoTrend.Set(false);
            }

			else if (noTrend && previousTrendisUp && resumeDownTrendAfterCounterTrendUp && LastSwingLowHigerThanPreviousLastSwingLow)
            {
				UpTrend.Set(false);
				DownTrend.Set(true);
				NoTrend.Set(false);
            }

        }

	}



}
