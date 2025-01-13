using SimpleCDN.Helpers;

namespace SimpleCDN.Tests.Unit
{
	public class RemoveFirstTests
	{
		[Test]
		public void RemoveFirst_Empty()
		{
			IEnumerable<int> source = [];
			Assert.That(source.RemoveFirst, Throws.InstanceOf<ArgumentOutOfRangeException>());
		}

		[Test]
		public void RemoveFirst_OneElement()
		{
			IEnumerable<int> source = [1];
			(int left, IEnumerable<int> right) = source.RemoveFirst();
			Assert.Multiple(() =>
			{
				Assert.That(left, Is.EqualTo(1));
				Assert.That(right, Is.Empty);
			});
		}

		[Test]
		public void RemoveFirst_TwoElements()
		{
			IEnumerable<int> source = [1, 2];
			(int left, IEnumerable<int> right) = source.RemoveFirst();
			Assert.Multiple(() =>
			{
				Assert.That(left, Is.EqualTo(1));
				Assert.That(right, Is.EqualTo([2]));
			});
		}

		[Test]
		public void RemoveFirst_ThreeElements()
		{
			IEnumerable<int> source = [1, 2, 3];
			(int left, IEnumerable<int> right) = source.RemoveFirst();
			Assert.Multiple(() =>
			{
				Assert.That(left, Is.EqualTo(1));
				Assert.That(right, Is.EqualTo([2, 3]));
			});
		}
	}
}
