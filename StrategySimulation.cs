using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using AgenaTrader.API;
using AgenaTrader.Custom;
using AgenaTrader.Plugins;
using AgenaTrader.Helper;

namespace AgenaTrader.UserCode
{

	public class StrategySimulation : UserIndicator
	{
		// **********************************************************
		// Deklarationen
		// **********************************************************

		// Startdatum
		DateTime StartBacktestDate = new DateTime(2019, 04, 01, 00, 00, 00);
		DateTime CurrentDate = new DateTime();


		// Deklaration Multibars
		private TimeFrame TF_Day = new TimeFrame("Day");
		private TimeFrame TF_Hour = new TimeFrame("1 Hour");


		// Deklaration Variablen
		int GWLSetup;
		int GWL_TrendSize;
		int SIGNAL_TrendSize;
		int IndexOfExecutionOrderBar;
		int CounterOrders;
		int CounterOrderFees;
		int CounterWinTrades;
		int CounterLossTrades;
		
		double BuyPrice;
		double InitialStopPriceLong;
		double SellPrice;
		double PositionSizeLong;
		double CashValue = 5000;
		double CashValueFinal;
		double BuyAndHold_BuyCapital;
		double BuyAndHold_SellCapital;
		double PositionSizeBuyAndHold;
		double AccWin;
		double AccLoss;
		double WinLoss;
		double SumWinLoss;
		double AverageWin;
		double AverageLoss;
		double ProfitFactor;
		double HitRate;

		bool InitialStopLongSignal;
		bool OutputConsoleIsShown = false;
		bool ReleaseGWL = false;
		bool TrendBreak = false;
		bool ReleaseSignal = false;
		bool CsvFileWasWritten = false;
		

		// Deklaration DatenSerien
		private DataSeries TrailingStopLastP3_TrendUp;
		private DataSeries TrailingStopLastP3_TrendDown;


		// CSV Datei
		string Filename = @"C:\Users\ghaid\Documents\AgenaTrader\UserCode\Indicators\Backtests\Backtest.csv";
		string CsvText;
		

		// Deklaration Objekte
		OrderStates OrderState = new OrderStates();
		TrendPhases TrendPhase = new TrendPhases();



		// **********************************************************
		// Agena Script - Ereignisse
		// **********************************************************
		protected override void OnBarsRequirements()
		{
			base.OnBarsRequirements();
			Add(TF_Day);
			Add(TF_Hour);
		}


		protected override void OnInit()
		{
			CalculateOnClosedBar = true;
			IsOverlay = true;

			TrailingStopLastP3_TrendUp = new DataSeries(this);
			TrailingStopLastP3_TrendDown = new DataSeries(this);
		}

		protected override void OnCalculate()
		{
			// Starte Simulation - Datum
			CurrentDate = Bars.GetTime(ProcessingBarIndex);
			if (CurrentDate.CompareTo(StartBacktestDate) > 0)
			{
				Main();
			}
		}


		// **********************************************************
		// Handelssystem
		// **********************************************************
		private void Main()
		{
			// Konfiguration Parameter Handelssystem 
			GWLSetup = 1;                                    
			GWL_TrendSize = 2;                              
			SIGNAL_TrendSize = 3;
			
			
			// Deklaration TimeFrames
			var itemDay = MultiBars.GetBarsItem(TF_Day);
			var itemHour = MultiBars.GetBarsItem(TF_Hour);
			

			// Deklaration Indikatoren - Übergeordneter Trend
			var GWL_TrendDirection = P123Adv(itemDay.Close, GWL_TrendSize).TrendDirection;
			var GWL_IsTrendValid = P123Adv(itemDay.Close, GWL_TrendSize).IsTrendValid;
			var GWL_LastP2 = P123Adv(itemDay.Close, GWL_TrendSize).P2Price;


			// Deklaration Indikatoren - Signalebene 
			var SIGNAL_TrendDirection = P123Adv(itemHour.Close, SIGNAL_TrendSize).TrendDirection;
			var SIGNAL_IsTrendValid = P123Adv(itemHour.Close, SIGNAL_TrendSize).IsTrendValid;
			var SIGNAL_LastP2 = P123Adv(itemHour.Close, SIGNAL_TrendSize).P2Price;
			var SIGNAL_LastP3 = P123Adv(itemHour.Close, SIGNAL_TrendSize).ValidP3Price;
			
			

			// **********************************************************
			// Filter - Handelsfreigabe aus übergeordnetem Trend
			// **********************************************************
			switch (GWLSetup)
			{
				case 0:     // Keine Berücksichtigung übergeordneter Trend
					ReleaseGWL = true;
					break;

				case 1:     // Freigabe bei Trendrichtung UpTrend
					GWLTrendUp(GWL_TrendDirection, GWL_IsTrendValid);
					break;

				case 2:     // Freigabe bei Trendrichtung UpTrend und Kurs unterhalb letzter Punkt 2 GWL
					GWLTrendUp_PriceUnderLastP2(GWL_TrendDirection, GWL_LastP2);
					break;
			}



			// **********************************************************
			// Trendbestimmung - Signalebene
			// **********************************************************
			TrendDetermination(SIGNAL_TrendDirection, SIGNAL_LastP3, SIGNAL_IsTrendValid);



			// **********************************************************
			// Visualisierung Trades
			// **********************************************************
			if (OrderState.IsLong())
			{
				AddChartRegion("Region1" + ProcessingBarIndex, 1, 0, EMA(High, 2), EMA(Low, 2), Color.Transparent, Color.Blue, 80);
			}

			

			// **********************************************************
			// Handeln
			// **********************************************************

			// -------------------- KAUFEN --------------------
			if (TrendPhase.UpTrendIsBroken())
			{
				ReleaseSignal = true;
			}

			if (OrderState.IsFlat() && TrendPhase.TrendIsUp() && ReleaseGWL)
			{
				bool BuySignal1 = High[0] > SIGNAL_LastP2[1];
				if (ReleaseSignal && BuySignal1)
				{
					BuyPrice = SIGNAL_LastP2[1];
					InitialStopPriceLong = SIGNAL_LastP3[1];
					PositionSizeLong = GetPositionSizeLong(BuyPrice, InitialStopPriceLong);
					Buy();
					CalculateBuyAndHoldBuy(BuyPrice);
					ReleaseSignal = false;
				}
			}


			// -------------------- INITIAL STOP -----------------
			if (OrderState.IsLong())
			{
				InitialStopLongSignal = Low[0] <= InitialStopPriceLong;
				if (InitialStopLongSignal)
				{
					InitialStopLong();
				}
			}


			// -------------------- VERKAUFEN --------------------
			if (OrderState.IsLong())
			{
				if (TrendPhase.UpTrendIsBroken())
				{
					SellPrice = TrailingStopLastP3_TrendUp[1];
					Sell();
				}
			}


			// ---------------- KONSOLENAUSGABE, CSV -------------
			OutputConsole();
			WriteCsvFile();

		}




		// **********************************************************
		// Übergeordneter Trend - Handelsfreigabe
		// **********************************************************
		private void GWLTrendUp(SByteSeries GWL_TrendDirection, BoolSeries GWL_IsTrendValid)
		{
			bool TrendUp = GWL_TrendDirection[0] == 1 && GWL_IsTrendValid[0];
			if (TrendUp)
			{
				ReleaseGWL = true;
			}
			else
			{
				ReleaseGWL = false;
			}
		}

		private void GWLTrendUp_PriceUnderLastP2(SByteSeries GWL_TrendDirection, DataSeries GWL_LastP2)
		{
			bool TrendUp = GWL_TrendDirection[0] == 1 && Close[0] < GWL_LastP2[0];
			if (TrendUp)
			{
				ReleaseGWL = true;
			}
			else
			{
				ReleaseGWL = false;
			}
		}


		// **********************************************************
		// Bestimmung Trendphase
		// **********************************************************
		private void TrendDetermination(SByteSeries SIGNAL_TrendDirection, DataSeries SIGNAL_LastP3, BoolSeries SIGNAL_IsTrendValid)
		{
			if (TrendPhase.IsInit())
			{
				// Entstehung Aufwärtstrend
				bool TrendIsUp = SIGNAL_TrendDirection[0] == 1;
				bool TrendUpIsValid = SIGNAL_IsTrendValid[0];
				if (TrendIsUp && TrendUpIsValid)
				{
					TrendPhase.SetTrendUp();
				}

				// Entstehung Abwärtstrend
				bool TrendIsDown = SIGNAL_TrendDirection[0] == -1;
				bool TrendDownIsValid = SIGNAL_IsTrendValid[0];
				if (TrendIsDown && TrendDownIsValid)
				{
					TrendPhase.SetTrendDown();
				}
			}

						
			if (TrendPhase.UpTrendIsBroken())
			{
				// Entstehung Aufwärtstrend
				bool TrendIsUp = SIGNAL_TrendDirection[0] == 1;
				bool TrendUpIsValid = SIGNAL_IsTrendValid[0];
				if (TrendIsUp && TrendUpIsValid)
				{
					TrendPhase.SetTrendUp();
				}

				// Entstehung Abwärtstrend
				bool TrendIsDown = SIGNAL_TrendDirection[0] == -1;
				bool TrendDownIsValid = SIGNAL_IsTrendValid[0];
				if (TrendIsDown && TrendDownIsValid)
				{
					TrendPhase.SetTrendDown();
				}
			}


			if (TrendPhase.DownTrendIsBroken())
			{
				// Entstehung Aufwärtstrend
				bool TrendIsUp = SIGNAL_TrendDirection[0] == 1;
				bool TrendUpIsValid = SIGNAL_IsTrendValid[0];
				if (TrendIsUp && TrendUpIsValid)
				{
					TrendPhase.SetTrendUp();
					TrendBreak = false;
				}

				// Entstehung Abwärtstrend
				bool TrendIsDown = SIGNAL_TrendDirection[0] == -1;
				bool TrendDownIsValid = SIGNAL_IsTrendValid[0];
				if (TrendIsDown && TrendDownIsValid)
				{
					TrendPhase.SetTrendDown();
				}
			}


			if (TrendPhase.TrendIsUp())
			{
				TrailingStopLastP3_TrendUp.Set(SIGNAL_LastP3[0]);

				// Setzte Trailing-Stop
				bool LastP3HasNoValue = TrailingStopLastP3_TrendUp[0] == 0;
				if (LastP3HasNoValue)
				{
					TrailingStopLastP3_TrendUp.Set(TrailingStopLastP3_TrendUp[1]);
				}

				// Setze Trailing-Stop
				bool TrendIsDown = SIGNAL_TrendDirection[0] == -1;
				if (TrendIsDown)
				{
					TrailingStopLastP3_TrendUp.Set(TrailingStopLastP3_TrendUp[1]);
				}

				// Trendbruch
				bool TrendBreak = Low[0] < TrailingStopLastP3_TrendUp[0] && Low[1] >= TrailingStopLastP3_TrendUp[1];
				if (TrendBreak)
				{
					TrendPhase.SetUpTrendIsBroken();
				}
								
			}


			if (TrendPhase.TrendIsDown())
			{
				TrailingStopLastP3_TrendDown.Set(SIGNAL_LastP3[0]);

				// Setze Trailing-Stop
				bool LastP3HasNoValue = TrailingStopLastP3_TrendDown[0] == 0;
				if (LastP3HasNoValue)
				{
					TrailingStopLastP3_TrendDown.Set(TrailingStopLastP3_TrendDown[1]);
				}

				// Setzte Trailing-Stop
				bool TrendIsUp = SIGNAL_TrendDirection[0] == 1;
				if (TrendIsUp)
				{
					TrailingStopLastP3_TrendDown.Set(TrailingStopLastP3_TrendDown[1]);
				}

				// Trendbruch
				bool TrendBreakUp = High[0] > TrailingStopLastP3_TrendDown[0] && High[1] <= TrailingStopLastP3_TrendDown[1];
				if (TrendBreakUp)
				{
					TrendPhase.SetDownTrendIsBroken();
				}
			}

		}




		// **********************************************************
		// Kaufen - Long
		// **********************************************************
		private void Buy()
		{
			OrderState.SetLong();
			CounterOrders += 1;
			CounterOrderFees += 4;
			IndexOfExecutionOrderBar = ProcessingBarIndex;

			AddChartDiamond("BuyDiamond1" + ProcessingBarIndex, 0, BuyPrice, Color.Green);
			AddChartLine("BuyLine1" + ProcessingBarIndex, true, 0, BuyPrice, -5, BuyPrice, Color.Green, DashStyle.Solid, 3);
			AddChartText("BuyText1" + ProcessingBarIndex, "Buy" + "(Anz.: " + PositionSizeLong.ToString("0.0") + ")  " + CounterOrders.ToString(), -5, BuyPrice, Color.Green);

			AddChartDiamond("InitialStopDiamond1" + ProcessingBarIndex, 0, InitialStopPriceLong, Color.Orange);
			AddChartLine("InitialStopLine1" + ProcessingBarIndex, true, 0, InitialStopPriceLong, -5, InitialStopPriceLong, Color.Orange, DashStyle.Solid, 3);
			AddChartText("InitialStopText1" + ProcessingBarIndex, "Initial Stop", -5, InitialStopPriceLong, Color.Orange);
		}



		// **********************************************************
		// Initial Stop - Long
		// **********************************************************
		private void InitialStopLong()
		{
			OrderState.SetFlat();
			SellPrice = InitialStopPriceLong;
			CalculateWinLoss();

			AddChartDiamond("BuyDiamond2" + ProcessingBarIndex, 0, InitialStopPriceLong, Color.Red);
			AddChartLine("BuyLine2" + ProcessingBarIndex, true, 0, InitialStopPriceLong, -5, InitialStopPriceLong, Color.Red, DashStyle.Solid, 3);
			AddChartText("BuyText2" + ProcessingBarIndex, "Sell (Anz.: (" + PositionSizeLong + ")  " + WinLoss.ToString("0.00") + " $)", -5, InitialStopPriceLong, Color.Red);
		}



		// **********************************************************
		// Verkaufen - Long
		// **********************************************************
		private void Sell()
		{
			OrderState.SetFlat();
			CalculateWinLoss();

			AddChartDiamond("SellDiamond1" + ProcessingBarIndex, 0, SellPrice, Color.Red);
			AddChartLine("SellLine1" + ProcessingBarIndex, true, 0, SellPrice, -5, SellPrice, Color.Red, DashStyle.Solid, 3);
			AddChartText("SellText1" + ProcessingBarIndex, "Sell (Anz.: (" + PositionSizeLong + ")  " + WinLoss.ToString("0.00") + " $)", -5, SellPrice, Color.Red);
		}

			

		// **********************************************************
		// Berechne Positionsgröße 
		// **********************************************************
		private double GetPositionSizeLong(double BuyPrice, double InitialStopPriceLong)
		{
			double Risk = BuyPrice - InitialStopPriceLong;
			double RiskAmountPerTrade = CashValue * 0.02;
			double PositionSizeLong = Math.Round(RiskAmountPerTrade / Risk);

			return PositionSizeLong;
		}
		


		// **********************************************************
		// Berechne Gewinn - Verlust 
		// **********************************************************
		private void CalculateWinLoss()
		{
			double Win;
			double Loss;

			bool WinningTrade = SellPrice > BuyPrice;
			if (WinningTrade)
			{
				Win = (SellPrice - BuyPrice) * PositionSizeLong;
				AccWin += Win;
				CounterWinTrades += 1;
				WinLoss = Win;
				CashValue += Win;
			}

			bool LoosingTrade = SellPrice <= BuyPrice; 
			if (LoosingTrade)
			{
				Loss = (SellPrice - BuyPrice) * PositionSizeLong;
				AccLoss += Loss;
				CounterLossTrades += 1;
				WinLoss = Loss;
				CashValue += Loss;
			}
		}


		
		// **********************************************************
		// Berechne Gewinn - Buy and Hold 
		// **********************************************************
		private void CalculateBuyAndHoldBuy(double BuyPrice)
		{
			if (CounterOrders == 1)
			{
				PositionSizeBuyAndHold = Math.Round(CashValue / BuyPrice);
				BuyAndHold_BuyCapital = BuyPrice * PositionSizeBuyAndHold;
			}
		}

		private double CalculateBuyAndHoldSell()
		{
			double BuyAndHoldWin;
			BuyAndHold_SellCapital = Close[0] * PositionSizeBuyAndHold;
			BuyAndHoldWin = BuyAndHold_SellCapital - BuyAndHold_BuyCapital;

			return BuyAndHoldWin;
		}



		// **********************************************************
		// Ausgabe Konsole
		// **********************************************************
		private void OutputConsole()
		{
			if (IsProcessingBarIndexLast && !OutputConsoleIsShown)
			{
				SumWinLoss = AccWin + AccLoss - CounterOrderFees;
				AverageWin = AccWin / CounterWinTrades;
				AverageLoss = AccLoss / CounterLossTrades;
				ProfitFactor = (AccWin) / Math.Abs(AccLoss);
				HitRate = (Convert.ToSingle(CounterWinTrades) / Convert.ToSingle(CounterOrders)) * 100;
				CashValueFinal = CashValue - CounterOrderFees;
				OutputConsoleIsShown = true;


				Print("-------------------------------------------------------------");
				Print("Kontostand: " + CashValueFinal.ToString("0.00"));
				Print("Trefferquote: " + HitRate.ToString("0.0") + " %");
				Print("Anzahl Loss-Trades: " + CounterLossTrades);
				Print("Anzahl Win-Trades: " + CounterWinTrades);
				Print("Anzahl Orders Gesamt: " + CounterOrders);
				Print("");
				Print("Profit Faktor: " + ProfitFactor.ToString("0.00"));
				Print("Durchschnittl. Verlust: " + AverageLoss.ToString("0.00"));
				Print("Durchschnittl. Gewinn: " + AverageWin.ToString("0.00"));

				Print("");
				Print("G/V Buy and Hold: " + CalculateBuyAndHoldSell().ToString("0.00"));
				Print("G/V Gesamt: " + SumWinLoss.ToString("0.00"));
				Print("Summe Ordergebühren: " + CounterOrderFees.ToString("0.00"));
				Print("Summe Verluste: " + AccLoss.ToString("0.00"));
				Print("Summe Gewinne: " + AccWin.ToString("0.00"));
				Print("");
				Print("Startdatum: " + StartBacktestDate);
				Print("Instrument: " + Instrument.Name + " (" + TimeFrame + ")");
			}
		}


		// **********************************************************
		// Schreibe CSV-Datei
		// **********************************************************
		private void WriteCsvFile()
		{
			if (IsProcessingBarIndexLast && !CsvFileWasWritten)
			{
				CsvText = Instrument.Name + ";" + AccWin.ToString("0.00") + ";" + AccLoss.ToString("0.00") + ";" + CounterOrderFees.ToString("0.00") + ";" + SumWinLoss.ToString("0.00") + ";" + AverageWin.ToString("0.00") + ";" + AverageLoss.ToString("0.00") + ";" + ProfitFactor.ToString("0.00") + ";" + CounterOrders + ";" + CounterWinTrades + ";" + CounterLossTrades + ";" + HitRate.ToString("0.00");
				try
				{
					File.AppendAllText(Filename, CsvText + Environment.NewLine);
				}
				catch
				{
					Print("Error. CSV cannot be opened.");
				}

				CsvFileWasWritten = true;
			}
		}

		

		// **********************************************************
		// Klassen
		// **********************************************************
		class OrderStates
		{
			// Attribute
			private bool _IsLong = false;
			private bool _IsFlat = true;
			
			// Konstruktor
			public OrderStates()
			{

			}

			// Setter
			public void SetLong()
			{
				_IsLong = true;
				_IsFlat = false;
			}

			public void SetFlat()
			{
				_IsLong = false;
				_IsFlat = true;
			}
			
			// Getter
			public bool IsLong()
			{
				return _IsLong;
			}

			public bool IsFlat()
			{
				return _IsFlat;
			}

		}


		class TrendPhases
		{
			// Attribute
			private bool _TrendIsUp = false;
			private bool _TrendIsDown = false;
			private bool _UpTrendIsBroken = false;
			private bool _DownTrendIsBroken = false;
			private bool _Init = true;

			// Konstruktor
			public TrendPhases()
			{

			}

			// Setter
			public void SetTrendUp()
			{
				_TrendIsUp = true;
				_TrendIsDown = false;
				_UpTrendIsBroken = false;
				_DownTrendIsBroken = false;
				_Init = false;
			}

			public void SetTrendDown()
			{
				_TrendIsUp = false;
				_TrendIsDown = true;
				_UpTrendIsBroken = false;
				_DownTrendIsBroken = false;
				_Init = false;
			}

			public void SetUpTrendIsBroken()
			{
				_TrendIsUp = false;
				_TrendIsDown = false;
				_UpTrendIsBroken = true;
				_DownTrendIsBroken = false;
				_Init = false;
			}

			public void SetDownTrendIsBroken()
			{
				_TrendIsUp = false;
				_TrendIsDown = false;
				_UpTrendIsBroken = false;
				_DownTrendIsBroken = true;
				_Init = false;
			}

			
			// Getter
			public bool TrendIsUp()
			{
				return _TrendIsUp;
			}

			public bool TrendIsDown()
			{
				return _TrendIsDown;
			}

			public bool UpTrendIsBroken()
			{
				return _UpTrendIsBroken;
			}

			public bool DownTrendIsBroken()
			{
				return _DownTrendIsBroken;
			}

			public bool IsInit()
			{
				return _Init;
			}
		}

	}

}