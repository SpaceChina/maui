using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Graphics;
using ALayoutDirection = Android.Views.LayoutDirection;
using ATextDirection = Android.Views.TextDirection;
using AView = Android.Views.View;

namespace Microsoft.Maui
{
	public static partial class ViewExtensions
	{
		const int DefaultAutomationTagId = -1;

		public static int AutomationTagId { get; set; } = DefaultAutomationTagId;

		public static void UpdateIsEnabled(this AView nativeView, IView view)
		{
			nativeView.Enabled = view.IsEnabled;
		}

		public static void UpdateVisibility(this AView nativeView, IView view)
		{
			nativeView.Visibility = view.Visibility.ToNativeVisibility();
		}

		public static void UpdateClip(this AView nativeView, IView view)
		{
			if (nativeView is WrapperView wrapper)
				wrapper.Clip = view.Clip;
		}

		public static ViewStates ToNativeVisibility(this Visibility visibility)
		{
			return visibility switch
			{
				Visibility.Hidden => ViewStates.Invisible,
				Visibility.Collapsed => ViewStates.Gone,
				_ => ViewStates.Visible,
			};
		}

		public static void UpdateBackground(this AView nativeView, IView view, Drawable? defaultBackgroundDrawable = null)
		{
			bool hasBorder = view.BorderShape != null && view.BorderBrush != null && view.BorderWidth > 0;

			if (hasBorder)
				nativeView.UpdateMauiDrawable(view);
			else
			{
				var background = view.Background;

				if (background.IsNullOrEmpty())
				{
					nativeView.Background = defaultBackgroundDrawable;
					return;
				}

				if (background is SolidPaint solidPaint)
				{
					Color backgroundColor = solidPaint.Color;

					if (backgroundColor != null)
						nativeView.SetBackgroundColor(backgroundColor.ToNative());
				}
				else
				{
					if (background!.ToDrawable(nativeView.Context) is Drawable drawable)
						nativeView.Background = drawable;
				}
			}
		}
		
		public static void UpdateBorderBrush(this AView nativeView, IView view)
		{
			var borderBrush = view.BorderBrush;
			MauiDrawable? background = nativeView.Background as MauiDrawable;

			if (background == null && borderBrush.IsNullOrEmpty())
				return;

			nativeView.UpdateMauiDrawable(view);
		}

		public static void UpdateBorderWidth(this AView nativeView, IView view)
		{
			var borderWidth = view.BorderWidth;
			MauiDrawable? background = nativeView.Background as MauiDrawable;

			if (background == null && borderWidth <= 0)
				return;

			nativeView.UpdateMauiDrawable(view);
		}

		public static void UpdateBorderDashArray(this AView nativeView, IView view)
		{
			var borderDashArray = view.BorderDashArray;
			MauiDrawable? background = nativeView.Background as MauiDrawable;

			if (background == null && (borderDashArray == null || borderDashArray.Length == 0))
				return;

			nativeView.UpdateMauiDrawable(view);
		}

		public static void UpdateBorderDashOffset(this AView nativeView, IView view) 
		{
			var borderDashOffset = view.BorderDashOffset;
			MauiDrawable? background = nativeView.Background as MauiDrawable;

			if (background == null && borderDashOffset == 0)
				return;

			nativeView.UpdateMauiDrawable(view);
		}

		public static void UpdateBorderShape(this AView nativeView, IView view)
		{
			var borderShape = view.BorderShape;
			MauiDrawable? background = nativeView.Background as MauiDrawable;

			if (background == null && borderShape == null)
				return;

			nativeView.UpdateMauiDrawable(view);
		}

		public static void UpdateOpacity(this AView nativeView, IView view)
		{
			nativeView.Alpha = (float)view.Opacity;
		}

		public static void UpdateFlowDirection(this AView nativeView, IView view)
		{
			// I realize I could call this method as an extension method
			// But I'm being explicit so if the TextViewExtensions version gets deleted
			// we'll get a compile time exception opposed to an infinite loop
			if (nativeView is TextView textview)
			{
				TextViewExtensions.UpdateFlowDirection(textview, view);
				return;
			}

			if (view.FlowDirection == view.Handler?.MauiContext?.GetFlowDirection() ||
				view.FlowDirection == FlowDirection.MatchParent)
			{
				nativeView.LayoutDirection = ALayoutDirection.Inherit;
			}
			else if (view.FlowDirection == FlowDirection.RightToLeft)
			{
				nativeView.LayoutDirection = ALayoutDirection.Rtl;
			}
			else if (view.FlowDirection == FlowDirection.LeftToRight)
			{
				nativeView.LayoutDirection = ALayoutDirection.Ltr;
			}
		}

		public static bool GetClipToOutline(this AView view)
		{
			return view.ClipToOutline;
		}

		public static void SetClipToOutline(this AView view, bool value)
		{
			view.ClipToOutline = value;
		}

		public static void UpdateAutomationId(this AView nativeView, IView view)
		{
			if (AutomationTagId == DefaultAutomationTagId)
			{
				AutomationTagId = Resource.Id.automation_tag_id;
			}

			nativeView.SetTag(AutomationTagId, view.AutomationId);
		}

		public static void InvalidateMeasure(this AView nativeView, IView view)
		{
			nativeView.RequestLayout();
		}

		public static void UpdateWidth(this AView nativeView, IView view)
		{
			// GetDesiredSize will take the specified Width into account during the layout
			if (!nativeView.IsInLayout)
			{
				nativeView.RequestLayout();
			}
		}

		public static void UpdateHeight(this AView nativeView, IView view)
		{
			// GetDesiredSize will take the specified Height into account during the layout
			if (!nativeView.IsInLayout)
			{
				nativeView.RequestLayout();
			}
		}

		public static void RemoveFromParent(this AView view)
		{
			if (view == null)
				return;

			if (view.Parent == null)
				return;

			((ViewGroup)view.Parent).RemoveView(view);
		}

		internal static void UpdateMauiDrawable(this AView nativeView, IView view)
		{
			bool hasBorder = view.BorderShape != null && view.BorderBrush != null && view.BorderWidth > 0;

			if (!hasBorder)
				return;

			MauiDrawable? mauiDrawable = nativeView.Background as MauiDrawable;

			if (mauiDrawable == null)
			{
				mauiDrawable = new MauiDrawable(nativeView.Context);

				nativeView.Background = mauiDrawable;
			}

			mauiDrawable.SetBackground(view.Background);
			mauiDrawable.SetBorderBrush(view.BorderBrush);
			mauiDrawable.SetBorderWidth(view.BorderWidth);
			mauiDrawable.SetBorderDash(view.BorderDashArray, view.BorderDashOffset);
			mauiDrawable.SetBorderShape(view.BorderShape);
		}
	}
}
