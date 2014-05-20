﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;

using AdamsLair.WinForms.Drawing;

namespace AdamsLair.WinForms.TimelineControls
{
	[TimelineModelViewAssignment(typeof(ITimelineGraphTrackModel))]
	public class TimelineViewGraphTrack : TimelineViewTrack
	{
		public enum AdjustVerticalMode
		{
			GrowAndShrink,
			Grow,
			Shrink
		}
		public enum DrawingQuality
		{
			High,
			Low,

			Default = High
		}
		public enum PrecisionLevel
		{
			High,
			Medium,
			Low,

			Default = Medium
		}


		private static List<Type> availableViewGraphTypes = null;

		private	float					verticalUnitTop		= 1.0f;
		private	float					verticalUnitBottom	= -1.0f;
		private	List<TimelineViewGraph>	graphList			= new List<TimelineViewGraph>();
		private	DrawingQuality			curveQuality		= DrawingQuality.Default;
		private	PrecisionLevel			curvePrecision		= PrecisionLevel.Default;
		private	PrecisionLevel			envelopePrecision	= PrecisionLevel.Default;


		public new ITimelineGraphTrackModel Model
		{
			get { return base.Model as ITimelineGraphTrackModel; }
		}
		public IEnumerable<TimelineViewGraph> Graphs
		{
			get { return this.graphList; }
		}
		public float VerticalUnitTop
		{
			get { return this.verticalUnitTop; }
			set
			{
				if (this.verticalUnitTop != value)
				{
					this.verticalUnitTop = value;
					this.Invalidate();
				}
			}
		}
		public float VerticalUnitBottom
		{
			get { return this.verticalUnitBottom; }
			set
			{
				if (this.verticalUnitBottom != value)
				{
					this.verticalUnitBottom = value;
					this.Invalidate();
				}
			}
		}
		public DrawingQuality CurveQuality
		{
			get { return this.curveQuality; }
			set
			{
				if (this.curveQuality != value)
				{
					this.curveQuality = value;
					this.Invalidate();
				}
			}
		}
		public PrecisionLevel CurvePrecision
		{
			get { return this.curvePrecision; }
			set
			{
				if (this.curvePrecision != value)
				{
					this.curvePrecision = value;
					this.Invalidate();
				}
			}
		}
		public PrecisionLevel EnvelopePrecision
		{
			get { return this.envelopePrecision; }
			set
			{
				if (this.envelopePrecision != value)
				{
					this.envelopePrecision = value;
					this.Invalidate();
				}
			}
		}
		
		
		public TimelineViewGraph GetGraphByModel(ITimelineGraphModel graphModel)
		{
			return this.graphList.FirstOrDefault(t => t.Model == graphModel);
		}
		
		public float ConvertUnitsToPixels(float units)
		{
			return units * (float)(this.Height - 1) / (this.verticalUnitTop - this.verticalUnitBottom);
		}
		public float ConvertPixelsToUnits(float pixels)
		{
			return pixels * (this.verticalUnitTop - this.verticalUnitBottom) / (float)(this.Height - 1);
		}
		public float GetUnitAtPos(float y)
		{
			return this.verticalUnitTop + ((float)y * (this.verticalUnitBottom - this.verticalUnitTop) / (float)(this.Height - 1));
		}
		public float GetPosAtUnit(float unit)
		{
			return (float)(this.Height - 1) * ((unit - this.verticalUnitTop) / (this.verticalUnitBottom - this.verticalUnitTop));
		}
		public IEnumerable<TimelineViewRulerMark> GetVisibleRulerMarks()
		{
			const float BigMarkDelta = 0.00001f;
			float bigMarkRange = TimelineView.GetNiceMultiple(Math.Abs(this.verticalUnitTop - this.verticalUnitBottom) * 0.5f);
			float rulerStep = -TimelineView.GetNiceMultiple(Math.Abs(this.verticalUnitTop - this.verticalUnitBottom)) / 10.0f;

			Rectangle trackRect = this.ParentView.GetTrackRectangle(this);
			int lineIndex = 0;
			foreach (float unitValue in TimelineView.EnumerateRulerMarks(rulerStep, 0.0f, this.verticalUnitTop, this.verticalUnitBottom, 1))
			{
				float markY = this.GetPosAtUnit(unitValue) + trackRect.Y;

				TimelineViewRulerMarkWeight weight;
				if ((((unitValue + BigMarkDelta) % bigMarkRange) + bigMarkRange) % bigMarkRange <= BigMarkDelta * 2.0f)
					weight = TimelineViewRulerMarkWeight.Major;
				else
					weight = TimelineViewRulerMarkWeight.Regular;

				if (Math.Abs(unitValue - this.verticalUnitTop) >= rulerStep && Math.Abs(unitValue - this.verticalUnitBottom) >= rulerStep)
				{
					yield return new TimelineViewRulerMark(unitValue, markY, weight);
				}

				lineIndex++;
			}

			yield break;
		}
		public void AdjustVerticalUnits(AdjustVerticalMode adjustMode)
		{
			float targetTop;
			float targetBottom;

			if (this.graphList.Count == 0)
			{
				targetTop = 1.0f;
				targetBottom = -1.0f;
			}
			else
			{
				float minUnits = float.MaxValue;
				float maxUnits = float.MinValue;
				foreach (TimelineViewGraph graph in this.graphList)
				{
					ITimelineGraphModel graphModel = graph.Model;
					minUnits = Math.Min(minUnits, graphModel.GetMinValueInRange(graphModel.BeginTime, graphModel.EndTime));
					maxUnits = Math.Max(maxUnits, graphModel.GetMaxValueInRange(graphModel.BeginTime, graphModel.EndTime));
				}
				targetTop = TimelineView.GetNiceMultiple(maxUnits);
				targetBottom = TimelineView.GetNiceMultiple(minUnits);
			}

			switch (adjustMode)
			{
				default:
				case AdjustVerticalMode.GrowAndShrink:
					this.verticalUnitTop = targetTop;
					this.verticalUnitBottom = targetBottom;
					break;
				case AdjustVerticalMode.Grow:
					this.verticalUnitTop = Math.Max(this.verticalUnitTop, targetTop);
					this.verticalUnitBottom = Math.Min(this.verticalUnitBottom, targetBottom);
					break;
				case AdjustVerticalMode.Shrink:
					this.verticalUnitTop = Math.Min(this.verticalUnitTop, targetTop);
					this.verticalUnitBottom = Math.Max(this.verticalUnitBottom, targetBottom);
					break;
			}

			if (this.verticalUnitBottom == this.verticalUnitTop)
				this.verticalUnitTop += 1.0f;

			this.Invalidate();
		}

		protected override void CalculateContentWidth(out float beginTime, out float endTime)
		{
			base.CalculateContentWidth(out beginTime, out endTime);
			if (this.graphList.Count > 0)
			{
				beginTime = this.graphList.Min(g => g.Model.BeginTime);
				endTime = this.graphList.Max(g => g.Model.EndTime);
			}
			else
			{
				beginTime = 0.0f;
				endTime = 0.0f;
			}
		}

		protected override void OnModelChanged(TimelineTrackModelChangedEventArgs e)
		{
			if (e.OldModel != null)
			{
				ITimelineGraphTrackModel oldModel = e.OldModel as ITimelineGraphTrackModel;

				oldModel.GraphsAdded -= this.model_GraphsAdded;
				oldModel.GraphsRemoved -= this.model_GraphsRemoved;
				oldModel.GraphChanged -= this.model_GraphChanged;

				if (oldModel.Graphs.Any())
				{
					this.OnModelGraphsRemoved(new TimelineGraphCollectionEventArgs(oldModel.Graphs));
				}
			}
			if (e.Model != null)
			{
				ITimelineGraphTrackModel newModel = e.Model as ITimelineGraphTrackModel;

				if (newModel.Graphs.Any())
				{
					this.OnModelGraphsAdded(new TimelineGraphCollectionEventArgs(newModel.Graphs));
				}

				newModel.GraphsAdded += this.model_GraphsAdded;
				newModel.GraphsRemoved += this.model_GraphsRemoved;
				newModel.GraphChanged += this.model_GraphChanged;
			}
			base.OnModelChanged(e);
			this.AdjustVerticalUnits(AdjustVerticalMode.GrowAndShrink);
		}
		protected virtual void OnModelGraphsAdded(TimelineGraphCollectionEventArgs e)
		{
			foreach (ITimelineGraphModel graphModel in e.Graphs)
			{
				TimelineViewGraph graph = this.GetGraphByModel(graphModel);
				if (graph != null) continue;

				// Determine Type of the TimelineViewTrack matching the TimelineTrackModel
				if (availableViewGraphTypes == null)
				{
					availableViewGraphTypes = 
						AppDomain.CurrentDomain.GetAssemblies().
						SelectMany(a => a.GetExportedTypes()).
						Where(t => !t.IsAbstract && !t.IsInterface && typeof(TimelineViewGraph).IsAssignableFrom(t)).
						ToList();
				}
				Type viewGraphType = null;
				foreach (Type graphType in availableViewGraphTypes)
				{
					foreach (TimelineModelViewAssignmentAttribute attrib in graphType.GetCustomAttributes(true).OfType<TimelineModelViewAssignmentAttribute>())
					{
						foreach (Type validModelType in attrib.ValidModelTypes)
						{
							if (validModelType.IsInstanceOfType(graphModel))
							{
								viewGraphType = graphType;
								break;
							}
						}
						if (viewGraphType != null) break;
					}
					if (viewGraphType != null) break;
				}
				if (viewGraphType == null) continue;

				// Create TimelineViewTrack accordingly
				graph = viewGraphType.CreateInstanceOf() as TimelineViewGraph;
				graph.Model = graphModel;

				this.graphList.Add(graph);
				graph.ParentTrack = this;
			}

			this.Invalidate();
			this.UpdateContentWidth();
			this.AdjustVerticalUnits(AdjustVerticalMode.Grow);
		}
		protected virtual void OnModelGraphsRemoved(TimelineGraphCollectionEventArgs e)
		{
			foreach (ITimelineGraphModel graphModel in e.Graphs)
			{
				TimelineViewGraph graph = this.GetGraphByModel(graphModel);
				if (graph == null) continue;

				graph.ParentTrack = null;
				graph.Model = null;
				this.graphList.Remove(graph);
			}

			this.Invalidate();
			this.UpdateContentWidth();
			this.AdjustVerticalUnits(AdjustVerticalMode.Shrink);
		}
		protected internal override void OnPaint(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle rect = e.TargetRect;

			// Draw extended ruler markings in the background
			{
				Pen bigLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkMajor.ScaleAlpha(0.25f)));
				Pen medLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkRegular.ScaleAlpha(0.25f)));
				Pen minLinePen = new Pen(new SolidBrush(e.Renderer.ColorRulerMarkMinor.ScaleAlpha(0.25f)));

				// Horizontal ruler marks
				foreach (TimelineViewRulerMark mark in this.ParentView.GetVisibleRulerMarks())
				{
					Pen markPen;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markPen = bigLinePen;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
							markPen = medLinePen;
							break;
						case TimelineViewRulerMarkWeight.Minor:
							markPen = minLinePen;
							break;
					}

					e.Graphics.DrawLine(markPen, (int)mark.PixelValue, (int)rect.Top, (int)mark.PixelValue, (int)rect.Bottom);
				}

				// Vertical ruler marks
				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks())
				{
					Pen markPen;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markPen = bigLinePen;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
							markPen = medLinePen;
							break;
						case TimelineViewRulerMarkWeight.Minor:
							markPen = minLinePen;
							break;
					}

					e.Graphics.DrawLine(markPen, (int)rect.Left, (int)mark.PixelValue, (int)rect.Right, (int)mark.PixelValue);
				}
			}

			// Draw the graphs
			{
				float beginUnitX = Math.Max(-this.ParentView.UnitScroll, this.ContentBeginTime);
				float endUnitX = Math.Min(-this.ParentView.UnitScroll + this.ParentView.VisibleUnitWidth, this.ContentEndTime);
				foreach (TimelineViewGraph graph in this.graphList)
				{
					graph.OnPaint(new TimelineViewTrackPaintEventArgs(this, e.Graphics, rect, beginUnitX, endUnitX));
				}
			}

			// Draw top and bottom borders
			e.Graphics.DrawLine(new Pen(e.Renderer.ColorVeryDarkBackground), rect.Left, rect.Top, rect.Right, rect.Top);
			e.Graphics.DrawLine(new Pen(e.Renderer.ColorVeryDarkBackground), rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
		}
		protected internal override void OnPaintLeftSidebar(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintLeftSidebar(e);
			this.DrawRuler(e.Graphics, e.Renderer, e.TargetRect, true);
		}
		protected internal override void OnPaintRightSidebar(TimelineViewTrackPaintEventArgs e)
		{
			base.OnPaintRightSidebar(e);
			this.DrawRuler(e.Graphics, e.Renderer, e.TargetRect, false);
		}
		protected void DrawRuler(Graphics g, TimelineViewControlRenderer r, Rectangle rect, bool left)
		{
			string verticalTopText = string.Format("{0}", (float)Math.Round(this.verticalUnitTop, 2));
			string verticalBottomText = string.Format("{0}", (float)Math.Round(this.verticalUnitBottom, 2));
			SizeF verticalTopTextSize = g.MeasureString(verticalTopText, r.FontSmall);
			SizeF verticalBottomTextSize = g.MeasureString(verticalBottomText, r.FontSmall);

			// Draw background
			Rectangle borderRect;
			if (this.ParentView.BorderStyle != System.Windows.Forms.BorderStyle.None)
			{
				borderRect = new Rectangle(
					rect.X - (left ? 1 : 0),
					rect.Y,
					rect.Width + 1,
					rect.Height);
			}
			else
			{
				borderRect = rect;
			}
			g.FillRectangle(new SolidBrush(r.ColorVeryLightBackground), rect);
			r.DrawBorder(g, borderRect, Drawing.BorderStyle.Simple, BorderState.Normal);

			// Determine drawing geometry
			Rectangle rectTrackName;
			Rectangle rectUnitMarkings;
			Rectangle rectUnitRuler;
			{
				float markingRatio = 0.5f + 0.5f * (1.0f - Math.Max(Math.Min((float)rect.Height / 32.0f, 1.0f), 0.0f));
				rectTrackName = new Rectangle(
					rect.X, 
					rect.Y, 
					Math.Min(rect.Width, r.FontRegular.Height + 2), 
					rect.Height);
				rectUnitMarkings = new Rectangle(
					rect.Right - Math.Min((int)(rect.Width * markingRatio), 16),
					rect.Y,
					Math.Min((int)(rect.Width * markingRatio), 16),
					rect.Height);
				int maxUnitWidth = Math.Max(Math.Max(rectUnitMarkings.Width, (int)verticalTopTextSize.Width + 2), (int)verticalBottomTextSize.Width + 2);
				rectUnitRuler = new Rectangle(
					rect.Right - maxUnitWidth,
					rect.Y,
					maxUnitWidth,
					rect.Height);

				if (!left)
				{
					rectTrackName.X		= rect.Right - (rectTrackName	.X	- rect.Left) - rectTrackName	.Width;
					rectUnitMarkings.X	= rect.Right - (rectUnitMarkings.X	- rect.Left) - rectUnitMarkings	.Width;
					rectUnitRuler.X		= rect.Right - (rectUnitRuler	.X	- rect.Left) - rectUnitRuler	.Width;
				}
			}

			// Draw track name
			{
				Rectangle overlap = rectUnitMarkings;
				overlap.Intersect(rectTrackName);
				float overlapAmount = Math.Max(Math.Min((float)overlap.Width / (float)rectTrackName.Width, 1.0f), 0.0f);
				float textOverlapAlpha = (1.0f - (overlapAmount));

				StringFormat format = new StringFormat(StringFormat.GenericDefault);
				format.Trimming = StringTrimming.EllipsisCharacter;

				SizeF textSize = g.MeasureString(this.Model.TrackName, r.FontRegular, rectTrackName.Height, format);

				var state = g.Save();
				g.TranslateTransform(
					rectTrackName.X + (int)textSize.Height + 2, 
					rectTrackName.Y);
				g.RotateTransform(90);
				g.DrawString(
					this.Model.TrackName, 
					r.FontRegular, 
					new SolidBrush(Color.FromArgb((int)(textOverlapAlpha * 255), r.ColorText)), 
					new Rectangle(0, 0, rectTrackName.Height, rectTrackName.Width), 
					format);
				g.Restore(state);
			}

			// Draw vertical unit markings
			{
				Pen bigLinePen = new Pen(new SolidBrush(r.ColorRulerMarkMajor));
				Pen medLinePen = new Pen(new SolidBrush(r.ColorRulerMarkRegular));
				Pen minLinePen = new Pen(new SolidBrush(r.ColorRulerMarkMinor));

				// Static Top and Bottom marks
				SizeF textSize;
				textSize = g.MeasureString(verticalTopText, r.FontSmall);
				g.DrawString(verticalTopText, r.FontSmall, new SolidBrush(r.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Top);
				textSize = g.MeasureString(verticalBottomText, r.FontSmall);
				g.DrawString(verticalBottomText, r.FontSmall, new SolidBrush(r.ColorText), rectUnitMarkings.Right - textSize.Width, rectUnitMarkings.Bottom - textSize.Height - 1);

				// Dynamic Inbetween marks
				foreach (TimelineViewRulerMark mark in this.GetVisibleRulerMarks())
				{
					float markLen;
					Pen markPen;
					bool bigMark;
					switch (mark.Weight)
					{
						case TimelineViewRulerMarkWeight.Major:
							markLen = 0.5f;
							markPen = bigLinePen;
							bigMark = true;
							break;
						default:
						case TimelineViewRulerMarkWeight.Regular:
						case TimelineViewRulerMarkWeight.Minor:
							markLen = 0.25f;
							markPen = medLinePen;
							bigMark = false;
							break;
					}

					int borderDistInner = r.FontSmall.Height / 2;
					int borderDistOuter = r.FontSmall.Height / 2 + 15;
					float borderDist = (float)Math.Min(Math.Abs(mark.PixelValue - rect.Top), Math.Abs(mark.PixelValue - rect.Bottom));
					
					float markTopX;
					float markBottomX;
					if (left)
					{
						markTopX = rectUnitMarkings.Right - markLen * rectUnitMarkings.Width;
						markBottomX = rectUnitMarkings.Right;
					}
					else
					{
						markTopX = rectUnitMarkings.Left;
						markBottomX = rectUnitMarkings.Left + markLen * rectUnitMarkings.Width;
					}

					if (borderDist > borderDistInner)
					{
						float alpha = Math.Min(1.0f, (float)(borderDist - borderDistInner) / (float)(borderDistOuter - borderDistInner));
						Color markColor = Color.FromArgb((int)(alpha * markPen.Color.A), markPen.Color);
						Color textColor = Color.FromArgb((int)(alpha * markPen.Color.A), r.ColorText);

						g.DrawLine(new Pen(markColor), (int)markTopX, (int)mark.PixelValue, (int)markBottomX, (int)mark.PixelValue);

						if (bigMark)
						{
							string text = string.Format("{0}", (float)Math.Round(mark.UnitValue, 2));
							textSize = g.MeasureString(text, r.FontSmall);
							g.DrawString(
								text, 
								r.FontSmall, 
								new SolidBrush(textColor), 
								left ? markTopX - textSize.Width : markBottomX, 
								mark.PixelValue - textSize.Height * 0.5f);
						}
					}
				}
			}
		}

		private void model_GraphChanged(object sender, TimelineGraphRangeEventArgs e)
		{
			this.Invalidate(e.BeginTime, e.EndTime);
			this.UpdateContentWidth();
		}
		private void model_GraphsAdded(object sender, TimelineGraphCollectionEventArgs e)
		{
			this.OnModelGraphsAdded(e);
		}
		private void model_GraphsRemoved(object sender, TimelineGraphCollectionEventArgs e)
		{
			this.OnModelGraphsRemoved(e);
		}
	}
}
