namespace SimpleCDN.Tests.Unit
{
	/*
	 * It might be slightly overkill, but these tests are here to ensure that the constants are valid.
	 * For example, if the CDNRouteValueKey contains '{' or '}', it will cause issues with the URL generation.
	 * This is the easiest way of enforcing proper constants, as unit tests are run on every pull request.
	 * 
	 * In the future, this could obviously be expanded to include more validation.
	 */

	public class ConstantsValidationTests
	{
		[Test]
		public void ValidateConstants()
		{
			Assert.Multiple(() =>
			{
				Assert.That(GlobalConstants.SystemFilesRelativePath, Does.Not.EndWith("/").And.Not.StartWith("/"));
				Assert.That(GlobalConstants.CDNRouteValueKey, Is.Not.Null.And.Not.Empty);
				Assert.That(GlobalConstants.CDNRouteValueKey, Does.Not.Contain('{').And.Not.Contain('}'));
			});
		}
	}
}
