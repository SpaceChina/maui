using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Internals;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Microsoft.Maui.Controls
{
	public partial class NavigationPage : INavigationView
	{
		Thickness IView.Margin => Thickness.Zero;

		partial void Init()
		{
			PushRequested += (_, args) =>
			{
				List<IView> newStack = new List<IView>((this as INavigationView).NavigationStack);
				var request = new MauiNavigationRequestedEventArgs(newStack, args.Animated);
				Handler?.Invoke(nameof(INavigationView.RequestNavigation), request);

			};

			PopRequested += (_, args) =>
			{
				List<IView> newStack = new List<IView>((this as INavigationView).NavigationStack);
				newStack.Remove(args.Page);
				var request = new MauiNavigationRequestedEventArgs(newStack, args.Animated);
				Handler?.Invoke(nameof(INavigationView.RequestNavigation), request);
			};

			RemovePageRequested += (_, args) =>
			{
				List<IView> newStack = new List<IView>((this as INavigationView).NavigationStack);
				newStack.Remove(args.Page);
				var request = new MauiNavigationRequestedEventArgs(newStack, args.Animated);
				Handler?.Invoke(nameof(INavigationView.RequestNavigation), request);
			};

			_insertPageBeforeRequested += (_, args) =>
			{
				// TODO MAUI why is this the only one where the stack insert is delayed?
				Device.BeginInvokeOnMainThread(() =>
				{
					List<IView> newStack = new List<IView>((this as INavigationView).NavigationStack);
					var request = new MauiNavigationRequestedEventArgs(newStack, args.Animated);
					Handler?.Invoke(nameof(INavigationView.RequestNavigation), request);
				});
			};

			PopToRootRequested += (_, args) =>
			{
				List<IView> newStack = new List<IView>((this as INavigationView).NavigationStack);
				var request = new MauiNavigationRequestedEventArgs(newStack, args.Animated);
				Handler?.Invoke(nameof(INavigationView.RequestNavigation), request);
			};
		}

		protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
		{
			if (Content is IView view)
			{
				view.Measure(widthConstraint, heightConstraint);
			}

			return new Size(widthConstraint, heightConstraint);
		}

		protected override Size ArrangeOverride(Rectangle bounds)
		{
			Frame = this.ComputeFrame(bounds);

			if (Content is IView view)
			{
				_ = view.Arrange(Frame);
			}

			return Frame.Size;
		}

		void INavigationView.RequestNavigation(MauiNavigationRequestedEventArgs eventArgs)
		{
			Handler?.Invoke(nameof(INavigationView.RequestNavigation), eventArgs);
		}

		void INavigationView.NavigationFinished(IReadOnlyList<IView> newStack)
		{
			for (int i = 0; i < newStack.Count; i++)
			{
				var element = (Element)newStack[i];

				if (InternalChildren.Count < i)
					InternalChildren.Add(element);
				else if (InternalChildren[i] != element)
				{
					int index = InternalChildren.IndexOf(element);
					if (index >= 0)
					{
						// TODO MAUI Do we support move?
						InternalChildren.Move(index, i);
					}
					else
					{
						InternalChildren.Insert(i, element);
					}
				}
			}

			while (InternalChildren.Count > newStack.Count)
			{
				InternalChildren.RemoveAt(InternalChildren.Count - 1);
			}

			CurrentPage = (Page)newStack[newStack.Count - 1];
		}

		IView Content => this.CurrentPage;

		IReadOnlyList<IView> INavigationView.NavigationStack =>
			this.Navigation.NavigationStack;

		static void CurrentPagePropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			var np = (NavigationPage)bindable;

			if (oldValue is INotifyPropertyChanged ncpOld)
			{
				ncpOld.PropertyChanged -= np.CurrentPagePropertyChanged;
			}

			if (newValue is INotifyPropertyChanged ncpNew)
			{
				ncpNew.PropertyChanged += np.CurrentPagePropertyChanged;
			}
		}

		void CurrentPagePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.IsOneOf(NavigationPage.HasNavigationBarProperty,
				NavigationPage.HasBackButtonProperty,
				NavigationPage.TitleIconImageSourceProperty,
				NavigationPage.TitleViewProperty,
				NavigationPage.IconColorProperty) ||
				e.Is(Page.TitleProperty))
			{
				Handler?.UpdateValue(e.PropertyName);
			}
		}
	}

}