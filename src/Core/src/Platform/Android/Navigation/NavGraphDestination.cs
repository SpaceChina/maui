using System;
using System.Collections.Generic;
using System.Linq;
using AndroidX.Navigation;
using AView = Android.Views.View;
namespace Microsoft.Maui
{
	// TODO MAUI MAKE PRIVATE or make it proxy and probably rename
	public class NavGraphDestination : NavGraph
	{
		IView? _currentPage;
		internal IView CurrentPage 
		{
			get => _currentPage ?? throw new InvalidOperationException("CurrentPage cannot be null");
			private set => _currentPage = value;
		}

		internal IReadOnlyList<IView> NavigationStack { get; private set; } = new List<IView>();
		internal Dictionary<IView, int> Pages = new Dictionary<IView, int>();

		public NavGraphDestination(Navigator navGraphNavigator) : base(navGraphNavigator)
		{
			Id = AView.GenerateViewId();
		}

		internal bool? IsPopping { get; private set; }
		internal bool IsAnimated { get; private set; } = true;

		/*
		 * The important thing to know going into reading this method is that it's not possible to
		 * modify the backstack. You can only push and pop to and from the top of the stack.
		 * So if a user uses an API like `RemovePage` or `InsertPage` we will typically ignore processing those natively
		 * unless it requires changes to the NavBar (i.e removing the first page with only 2 pages on the stack).
		 * Once the user performs an operation that changes the currently visible page then we process any stack changes
		 * that have occurred.
		 * Let's say the user has pages A,B,C,D on the stack 
		 * If they remove Page B and Page C then we don't do anything. Then if the user pushes E onto the stack
		 * we just transform A,B,C,D into A,D,E.
		 * Natively that's a "pop" but we use the correct animation for a "push" so visually it looks like a push.
		 * This is also the reason why we aren't using the custom animation features on the navigation component itself.
		 * Because we might be natively popping but visually pushing.
		 * 
		 * The Fragments that are on the stack also do not have a hard connection to the page they originally rendereded.
		 * Whenever a fragment is the "visible" fragment it just figures out what the current page is and displays that.
		 * Fragments are recreated everytime they are pushed on the stack but the handler renderer is not.
		 * It's just attached to a new fragment
		 * */
		internal void ApplyNavigationRequest(
			IReadOnlyList<IView> newPageStack,
			bool animated,
			NavigationLayout navigationLayout)
		{
			var navController = navigationLayout.NavHost.NavController;

			// The incoming fragment uses these variables to pick the correct animation for the current
			// incoming navigation request
			if (newPageStack[newPageStack.Count - 1] == NavigationStack[NavigationStack.Count - 1])
				IsPopping = null;
			else
				IsPopping = newPageStack.Count < NavigationStack.Count;

			IsAnimated = animated;

			// If the new stack isn't changing the visible page or the app bar then we just ignore
			// the changes because there's no point to applying these to the native back stack
			// We only apply changes when the currently visible page changes and/or the appbar
			// will change (gain a back button)
			if (newPageStack[newPageStack.Count - 1] == NavigationStack[NavigationStack.Count - 1] &&
				newPageStack.Count > 1 &&
				NavigationStack.Count > 1)
			{
				UpdateNavigationStack(newPageStack);
				return;
			}

			var iterator = navigationLayout.NavHost.NavController.BackStack.Iterator();
			var fragmentNavDestinations = new List<FragmentNavDestination>();

			while (iterator.HasNext)
			{
				if (iterator.Next() is NavBackStackEntry nbse &&
					nbse.Destination is FragmentNavDestination nvd)
				{
					fragmentNavDestinations.Add(nvd);
				}
			}

			Pages.Clear();
			if (fragmentNavDestinations.Count < newPageStack.Count)
			{
				for (int i = 0; i < newPageStack.Count; i++)
				{
					if (fragmentNavDestinations.Count > i)
					{
						Pages.Add(newPageStack[i], fragmentNavDestinations[i].Id);
						fragmentNavDestinations[i].Page = newPageStack[i];
					}
					else
					{
						var dest = AddDestination(newPageStack[i], navigationLayout);
						Pages[newPageStack[i]] = dest.Id;
						navController.Navigate(dest.Id);
					}
				}
			}
			else if (newPageStack.Count == fragmentNavDestinations.Count)
			{
				int lastFragId = fragmentNavDestinations[newPageStack.Count - 1].Id;

				for (int i = 0; i < newPageStack.Count; i++)
				{
					Pages.Add(newPageStack[i], fragmentNavDestinations[i].Id);
					fragmentNavDestinations[i].Page = newPageStack[i];
				}

				navController.PopBackStack();
				navController.Navigate(lastFragId);
			}
			// user is popping to root
			else if (newPageStack.Count == 1)
			{
				// TODO MAUI work with cleaning up fragments before actually firing navigation
				Pages.Add(newPageStack[0], fragmentNavDestinations[0].Id);
				fragmentNavDestinations[0].Page = newPageStack[0];
				navController.PopBackStack(fragmentNavDestinations[0].Id, false);
			}
			else
			{
				int popToId = fragmentNavDestinations[newPageStack.Count - 1].Id;
				for (int i = 0; i < newPageStack.Count; i++)
				{
					Pages.Add(newPageStack[i], fragmentNavDestinations[i].Id);

					if (fragmentNavDestinations[i].Page != newPageStack[i])
						fragmentNavDestinations[i].Page = newPageStack[i];
				}

				navController.PopBackStack(popToId, false);
			}


			foreach (var thing in fragmentNavDestinations)
			{
				if (!Pages.Values.ToList().Contains(thing.Id))
				{
					this.Remove(thing);
				}
			}

			UpdateNavigationStack(newPageStack);
		}

		public FragmentNavDestination AddDestination(
			IView page,
			NavigationLayout navigationLayout)
		{
			var destination = new FragmentNavDestination(page, navigationLayout, this);
			AddDestination(destination);
			return destination;
		}

		// This occurs when the navigation page is first being renderer so we sync up the
		// Navigation Stack on the INavigationView to our native stack
		internal List<int> ApplyPagesToGraph(
			IReadOnlyList<IView> pages,
			NavigationLayout navigationLayout)
		{
			var navController = navigationLayout.NavHost.NavController;

			// We are subtracting one because the navgraph itself is the first item on the stack
			int NativeNavigationStackCount = navController.BackStack.Size() - 1;

			// set this to one because when the graph is first attached to the controller
			// it will add the graph and the first destination
			if (NativeNavigationStackCount < 0)
				NativeNavigationStackCount = 1;

			List<int> destinations = new List<int>();

			NavDestination navDestination;

			foreach (var page in pages)
			{
				navDestination =
						AddDestination(
							page,
							navigationLayout);

				destinations.Add(navDestination.Id);
			}

			StartDestination = destinations[0];
			navController.SetGraph(this, null);

			for (var i = NativeNavigationStackCount; i < pages.Count; i++)
			{
				var dest = destinations[i];
				navController.Navigate(dest);
			}

			UpdateNavigationStack(pages);
			return destinations;
		}

		void UpdateNavigationStack(IReadOnlyList<IView> newPageStack)
		{
			NavigationStack = new List<IView>(newPageStack);
			CurrentPage = NavigationStack[NavigationStack.Count - 1];
		}
	}
}
