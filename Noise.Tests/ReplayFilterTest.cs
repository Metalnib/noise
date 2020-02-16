using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Noise.Tests
{
	public class ReplayFilterTest
	{
		[Fact]
		public void TestFilter()
		{
			var filter = new ReplayFilterNoBitshift();

			Assert.True(filter.ValidateCounter(0));

			var result = filter.ValidateCounter(1);
			Assert.True(result);
			Assert.True(filter.ValidateCounter(2));

			result = filter.ValidateCounter(63);
			result = filter.ValidateCounter(64);

			result = filter.ValidateCounter(64);
			Assert.False(result);

			Assert.True(filter.ValidateCounter(2049)); //Move windows
			Assert.False(filter.ValidateCounter(64)); //Old packet

			Assert.True(filter.ValidateCounter(3000));
			Assert.False(filter.ValidateCounter(3000));
			Assert.False(filter.ValidateCounter(2049));
			Assert.True(filter.ValidateCounter(2050));
		}

		[Fact]
		public void TestFillFilter()
		{
			var filter = new ReplayFilterNoBitshift();
			for (ulong i = 0; i < 8200; i++)
			{
				Assert.True(filter.ValidateCounter(i));
			}

			for (ulong i = 0; i < 8200; i++)
			{
				Assert.False(filter.ValidateCounter(i));
			}

			Assert.True(filter.ValidateCounter(8201));
		}

		[Fact(Skip = "Takes too long to complete. And imposible with Uint64.MaxValue")]
		public void TestFullFill()
		{
			var filter = new ReplayFilterNoBitshift();
			for (ulong i = 0; i < uint.MaxValue; i++)
			{
				Assert.True(filter.ValidateCounter(i));
			}
		}
	}
}
