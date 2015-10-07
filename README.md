# NavigationPattern_Sample
This is a sample of code I have implemented before to apply a navigation design pattern across multiple navigation scenarios so user is navigated through many pages based on his selections.
The benefit of this pattern is that aspx pages are not aware with previous or next step. It just calls NavigateToNextStep method on its navigator class, passes its main user selections and its navigator class will decided where to go.

It consists of:
1- NavBase: The base class of navigation which defines the abstract methods to be implemented by child classes of the real navigation classes and common business methods between scenarios.
2- Three navigation classes for different scenarios which mainly inherit from NavBase and implement the abstract methods in addition to the methods needed specifically by each scenario.
3- Staging page which defines the needed navigation scenario to be instantiated based on a query string parameter received from the home page links.
