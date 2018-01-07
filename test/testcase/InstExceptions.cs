using System;
using testInsts;

namespace testcase
{
	internal class OVFTestAdd
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			checked
			{
				return (sbyte)(a + a);
			}
		}

		private static byte Test_byte(byte a)
		{
			checked
			{
				return (byte)(a + a);

			}
		}

		private static short Test_short(short a)
		{
			checked
			{
				return (short)(a + a);
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			checked
			{
				return (ushort)(a + a);
			}
		}

		private static int Test_int(int a)
		{
			checked
			{
				return a + a;
			}
		}

		private static uint Test_uint(uint a)
		{
			checked
			{
				return a + a;
			}
		}

		private static long Test_long(long a)
		{
			checked
			{
				return a + a;
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			checked
			{
				return a + a;
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2U));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2L));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2UL));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTestSub
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			checked
			{
				return (sbyte)(-1 - a - a);
			}
		}

		private static byte Test_byte(byte a)
		{
			checked
			{
				return (byte)(0 - a - a);
			}
		}

		private static short Test_short(short a)
		{
			checked
			{
				return (short)(-1 - a - a);
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			checked
			{
				return (ushort)(0 - a - a);
			}
		}

		private static int Test_int(int a)
		{
			checked
			{
				return -1 - a - a;
			}
		}

		private static uint Test_uint(uint a)
		{
			checked
			{
				return 0U - a - a;
			}
		}

		private static long Test_long(long a)
		{
			checked
			{
				return -1L - a - a;
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			checked
			{
				return 0UL - a - a;
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2U));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2L));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2UL));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTestMul
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			checked
			{
				return (sbyte)(a * 2);
			}
		}

		private static byte Test_byte(byte a)
		{
			checked
			{
				return (byte)(a * 2);
			}
		}

		private static short Test_short(short a)
		{
			checked
			{
				return (short)(a * 2);
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			checked
			{
				return (ushort)(a * 2);
			}
		}

		private static int Test_int(int a)
		{
			checked
			{
				return a * 2;
			}
		}

		private static uint Test_uint(uint a)
		{
			checked
			{
				return a * 2;
			}
		}

		private static long Test_long(long a)
		{
			checked
			{
				return a * 2;
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			checked
			{
				return a * 2;
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2U));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2L));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2UL));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTestDiv
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			checked
			{
				return (sbyte)(a / 0.5);
			}
		}

		private static byte Test_byte(byte a)
		{
			checked
			{
				return (byte)(a / 0.5);
			}
		}

		private static short Test_short(short a)
		{
			checked
			{
				return (short)(a / 0.5);
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			checked
			{
				return (ushort)(a / 0.5);
			}
		}

		private static int Test_int(int a)
		{
			checked
			{
				return (int)(a / 0.5);
			}
		}

		private static uint Test_uint(uint a)
		{
			checked
			{
				return (uint)(a / 0.5);
			}
		}

		private static long Test_long(long a)
		{
			checked
			{
				return (long)(a / 0.5);
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			checked
			{
				return (ulong)(a / 0.5);
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2U));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2L));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2UL));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest2Add
	{
		private static sbyte Test_sbyte()
		{

			sbyte a = 1 + sbyte.MaxValue / 2;
			checked
			{
				return (sbyte)(a + a);
			}
		}

		private static byte Test_byte()
		{

			byte a = 1 + byte.MaxValue / 2;
			checked
			{
				return (byte)(a + a);
			}
		}

		private static short Test_short()
		{

			short a = 1 + short.MaxValue / 2;
			checked
			{
				return (short)(a + a);
			}
		}

		private static ushort Test_ushort()
		{

			ushort a = 1 + ushort.MaxValue / 2;
			checked
			{
				return (ushort)(a + a);
			}
		}

		private static int Test_int()
		{

			int a = 1 + int.MaxValue / 2;
			checked
			{
				return a + a;
			}
		}

		private static uint Test_uint()
		{

			uint a = 1U + uint.MaxValue / 2U;
			checked
			{
				return a + a;
			}
		}

		private static long Test_long()
		{

			long a = 1L + long.MaxValue / 2L;
			checked
			{
				return a + a;
			}
		}

		private static ulong Test_ulong()
		{

			ulong a = 1UL + ulong.MaxValue / 2UL;
			checked
			{
				return a + a;
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte();
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte();
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short();
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort();
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int();
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint();
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long();
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong();
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest2Sub
	{
		private static sbyte Test_sbyte()
		{

			sbyte a = 1 + sbyte.MaxValue / 2;
			checked
			{
				return (sbyte)(-1 - a - a);
			}
		}

		private static byte Test_byte()
		{

			byte a = 1 + byte.MaxValue / 2;
			checked
			{
				return (byte)(0 - a - a);
			}
		}

		private static short Test_short()
		{

			short a = 1 + short.MaxValue / 2;
			checked
			{
				return (short)(-1 - a - a);
			}
		}

		private static ushort Test_ushort()
		{

			ushort a = 1 + ushort.MaxValue / 2;
			checked
			{
				return (ushort)(0 - a - a);
			}
		}

		private static int Test_int()
		{

			int a = 1 + int.MaxValue / 2;
			checked
			{
				return -1 - a - a;
			}
		}

		private static uint Test_uint()
		{

			uint a = 1U + uint.MaxValue / 2U;
			checked
			{
				return 0U - a - a;
			}
		}

		private static long Test_long()
		{

			long a = 1L + long.MaxValue / 2L;
			checked
			{
				return -1L - a - a;
			}
		}

		private static ulong Test_ulong()
		{

			ulong a = 1UL + ulong.MaxValue / 2UL;
			checked
			{
				return 0UL - a - a;
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte();
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte();
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short();
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort();
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int();
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint();
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long();
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong();
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest2Mul
	{
		private static sbyte Test_sbyte()
		{

			sbyte a = 1 + sbyte.MaxValue / 2;
			checked
			{
				return (sbyte)(a * 2);
			}
		}

		private static byte Test_byte()
		{

			byte a = 1 + byte.MaxValue / 2;
			checked
			{
				return (byte)(a * 2);
			}
		}

		private static short Test_short()
		{

			short a = 1 + short.MaxValue / 2;
			checked
			{
				return (short)(a * 2);
			}
		}

		private static ushort Test_ushort()
		{

			ushort a = 1 + ushort.MaxValue / 2;
			checked
			{
				return (ushort)(a * 2);
			}
		}

		private static int Test_int()
		{

			int a = 1 + int.MaxValue / 2;
			checked
			{
				return a * 2;
			}
		}

		private static uint Test_uint()
		{

			uint a = 1U + uint.MaxValue / 2U;
			checked
			{
				return a * 2;
			}
		}

		private static long Test_long()
		{

			long a = 1L + long.MaxValue / 2L;
			checked
			{
				return a * 2;
			}
		}

		private static ulong Test_ulong()
		{

			ulong a = 1UL + ulong.MaxValue / 2UL;
			checked
			{
				return a * 2;
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte();
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte();
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short();
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort();
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int();
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint();
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long();
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong();
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest2Div
	{
		private static sbyte Test_sbyte()
		{

			sbyte a = 1 + sbyte.MaxValue / 2;
			checked
			{
				return (sbyte)(a / 0.5);
			}
		}

		private static byte Test_byte()
		{

			byte a = 1 + byte.MaxValue / 2;
			checked
			{
				return (byte)(a / 0.5);
			}
		}

		private static short Test_short()
		{

			short a = 1 + short.MaxValue / 2;
			checked
			{
				return (short)(a / 0.5);
			}
		}

		private static ushort Test_ushort()
		{

			ushort a = 1 + ushort.MaxValue / 2;
			checked
			{
				return (ushort)(a / 0.5);
			}
		}

		private static int Test_int()
		{

			int a = 1 + int.MaxValue / 2;
			checked
			{
				return (int)(a / 0.5);
			}
		}

		private static uint Test_uint()
		{

			uint a = 1U + uint.MaxValue / 2U;
			checked
			{
				return (uint)(a / 0.5);
			}
		}

		private static long Test_long()
		{

			long a = 1L + long.MaxValue / 2L;
			checked
			{
				return (long)(a / 0.5);
			}
		}

		private static ulong Test_ulong()
		{

			ulong a = 1UL + ulong.MaxValue / 2UL;
			checked
			{
				return (ulong)(a / 0.5);
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte();
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte();
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short();
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort();
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int();
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint();
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long();
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong();
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest3Add
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			try
			{
				checked
				{
					a = (sbyte)(a + a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (sbyte)(a + a);
				}
			}
		}

		private static byte Test_byte(byte a)
		{
			try
			{
				checked
				{
					a = (byte)(a + a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (byte)(a + a);
				}
			}
		}

		private static short Test_short(short a)
		{
			try
			{
				checked
				{
					a = (short)(a + a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (short)(a + a);
				}
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			try
			{
				checked
				{
					a = (ushort)(a + a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (ushort)(a + a);
				}
			}
		}

		private static int Test_int(int a)
		{
			try
			{
				checked
				{
					a = a + a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a + a;
				}
			}
		}

		private static uint Test_uint(uint a)
		{
			try
			{
				checked
				{
					a = a + a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a + a;
				}
			}
		}

		private static long Test_long(long a)
		{
			try
			{
				checked
				{
					a = a + a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a + a;
				}
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			try
			{
				checked
				{
					a = a + a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a + a;
				}
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest3Sub
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			try
			{
				checked
				{
					a = (sbyte)(-1 - a - a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (sbyte)(-1 - a - a);
				}
			}
		}

		private static byte Test_byte(byte a)
		{
			try
			{
				checked
				{
					a = (byte)(0 - a - a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (byte)(0 - a - a);
				}
			}
		}

		private static short Test_short(short a)
		{
			try
			{
				checked
				{
					a = (short)(-1 - a - a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (short)(-1 - a - a);
				}
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			try
			{
				checked
				{
					a = (ushort)(0 - a - a);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (ushort)(0 - a - a);
				}
			}
		}

		private static int Test_int(int a)
		{
			try
			{
				checked
				{
					a = -1 - a - a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = -1 - a - a;
				}
			}
		}

		private static uint Test_uint(uint a)
		{
			try
			{
				checked
				{
					a = 0U - a - a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = 0U - a - a;
				}
			}
		}

		private static long Test_long(long a)
		{
			try
			{
				checked
				{
					a = -1L - a - a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = -1L - a - a;
				}
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			try
			{
				checked
				{
					a = 0UL - a - a;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = 0UL - a - a;
				}
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest3Mul
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			try
			{
				checked
				{
					a = (sbyte)(a * 2);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (sbyte)(a * 2);
				}
			}
		}

		private static byte Test_byte(byte a)
		{
			try
			{
				checked
				{
					a = (byte)(a * 2);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (byte)(a * 2);
				}
			}
		}

		private static short Test_short(short a)
		{
			try
			{
				checked
				{
					a = (short)(a * 2);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (short)(a * 2);
				}
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			try
			{
				checked
				{
					a = (ushort)(a * 2);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (ushort)(a * 2);
				}
			}
		}

		private static int Test_int(int a)
		{
			try
			{
				checked
				{
					a = a * 2;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a * 2;
				}
			}
		}

		private static uint Test_uint(uint a)
		{
			try
			{
				checked
				{
					a = a * 2U;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a * 2U;
				}
			}
		}

		private static long Test_long(long a)
		{
			try
			{
				checked
				{
					a = a * 2L;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a * 2L;
				}
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			try
			{
				checked
				{
					a = a * 2UL;
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = a * 2UL;
				}
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2));
				return 7;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	internal class OVFTest3Div
	{
		private static sbyte Test_sbyte(sbyte a)
		{
			try
			{
				checked
				{
					a = (sbyte)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (sbyte)(a / 0.5);
				}
			}
		}

		private static byte Test_byte(byte a)
		{
			try
			{
				checked
				{
					a = (byte)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (byte)(a / 0.5);
				}
			}
		}

		private static short Test_short(short a)
		{
			try
			{
				checked
				{
					a = (short)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (short)(a / 0.5);
				}
			}
		}

		private static ushort Test_ushort(ushort a)
		{
			try
			{
				checked
				{
					a = (ushort)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (ushort)(a / 0.5);
				}
			}
		}

		private static int Test_int(int a)
		{
			try
			{
				checked
				{
					a = (int)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (int)(a / 0.5);
				}
			}
		}

		private static uint Test_uint(uint a)
		{
			try
			{
				checked
				{
					a = (uint)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (uint)(a / 0.5);
				}
			}
		}

		private static long Test_long(long a)
		{
			try
			{
				checked
				{
					a = (long)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (long)(a / 0.5);
				}
			}
		}

		private static ulong Test_ulong(ulong a)
		{
			try
			{
				checked
				{
					a = (ulong)(a / 0.5);
					return a;
				}
			}
			catch (System.OverflowException)
			{
				return a;
			}
			finally
			{
				checked
				{
					a = (ulong)(a / 0.5);
				}
			}
		}

		public static int Entry()
		{
			try
			{
				byte a = Test_byte((byte)(1 + byte.MaxValue / 2));
				return 1;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				sbyte a = Test_sbyte((sbyte)(1 + sbyte.MaxValue / 2));
				return 2;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				short a = Test_short((short)(1 + short.MaxValue / 2));
				return 3;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				ushort a = Test_ushort((ushort)(1 + ushort.MaxValue / 2));
				return 4;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				int a = Test_int((int)(1 + int.MaxValue / 2));
				return 5;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				uint a = Test_uint((uint)(1U + uint.MaxValue / 2));
				return 6;
			}
			catch (System.OverflowException)
			{
			}

			try
			{
				long a = Test_long((long)(1L + long.MaxValue / 2));
				return 7;
			}
			catch (System.OverflowException)
			{

			}

			try
			{
				ulong a = Test_ulong((ulong)(1UL + ulong.MaxValue / 2));
				return 8;
			}
			catch (System.OverflowException)
			{
			}

			return 0;
		}
	}

	[CodeGen]
	static class TestInstExceptions
	{
		static int TestCkfinite()
		{
			try
			{
				TestInstructions.CkfiniteNaN();
				return 1;
			}
			catch (ArithmeticException)
			{
			}

			try
			{
				TestInstructions.CkfiniteNaND();
				return 2;
			}
			catch (ArithmeticException)
			{
			}

			try
			{
				TestInstructions.CkfinitePosInf();
				return 3;
			}
			catch (ArithmeticException)
			{
			}

			try
			{
				TestInstructions.CkfinitePosInfD();
				return 4;
			}
			catch (ArithmeticException)
			{
			}
			try
			{
				TestInstructions.CkfiniteNegInf();
				return 5;
			}
			catch (ArithmeticException)
			{
			}
			try
			{
				TestInstructions.CkfiniteNegInfD();
				return 6;
			}
			catch (ArithmeticException)
			{
			}

			return 0;
		}

		static int LoopAddOvf(byte lhs, byte rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (byte)(lhs + rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopAddOvf(ushort lhs, ushort rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (ushort)(lhs + rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopAddOvf(short lhs, short rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (short)(lhs + rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopAddOvf(int lhs, int rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs + rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopAddOvf(uint lhs, uint rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs + rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopAddOvf(long lhs, long rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs + rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopAddOvf(ulong lhs, ulong rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs + rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(byte lhs, byte rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (byte)(lhs - rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(ushort lhs, ushort rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (ushort)(lhs - rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(short lhs, short rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (short)(lhs - rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(int lhs, int rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs - rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(uint lhs, uint rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs - rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(long lhs, long rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs - rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopSubOvf(ulong lhs, ulong rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs - rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(byte lhs, byte rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (byte)(lhs * rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(ushort lhs, ushort rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (ushort)(lhs * rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(short lhs, short rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = (short)(lhs * rhs);
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(int lhs, int rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs * rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(uint lhs, uint rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs * rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(long lhs, long rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs * rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopMulOvf(ulong lhs, ulong rhs)
		{
			int counter = 0;
			try
			{
				checked
				{
					for (int i = 0; i < 999999; ++i)
					{
						lhs = lhs * rhs;
						counter++;
					}
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopConvInt(long num, long acc)
		{
			int counter = 0;
			int result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (int)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			if (result == 0)
				return -1;
			return counter;
		}

		static int LoopConvInt(ulong num, ulong acc)
		{
			int counter = 0;
			int result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (int)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			if (result == 0)
				return -1;
			return counter;
		}

		static int LoopConvInt(double num, double acc)
		{
			int counter = 0;
			int result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (int)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			if (result == 0)
				return -1;
			return counter;
		}

		static int LoopConvLong(ulong num, ulong acc)
		{
			int counter = 0;
			long result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (long)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			if (result == 0)
				return -1;
			return counter;
		}

		static int LoopConvLong(double num, double acc)
		{
			int counter = 0;
			long result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (long)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			if (result == 0)
				return -1;
			return counter;
		}

		static int LoopConvULong(long num, long acc)
		{
			int counter = 0;
			ulong result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (ulong)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int LoopConvULong(double num, double acc)
		{
			int counter = 0;
			ulong result = 0;
			try
			{
				for (int i = 0; i < 999999; ++i)
				{
					checked
					{
						result = (ulong)num;
					}
					num += acc;
					counter++;
				}
			}
			catch (OverflowException)
			{
			}
			return counter;
		}

		static int TestOverflow()
		{
			int res = LoopAddOvf((byte)120, (byte)1);
			if (res != 135)
				return 1;
			res = LoopAddOvf((byte)0, (byte)1);
			if (res != 255)
				return 2;
			res = LoopAddOvf((ushort)32760, (ushort)1);
			if (res != 32775)
				return 3;
			res = LoopAddOvf((ushort)0, (ushort)1);
			if (res != 65535)
				return 4;
			res = LoopAddOvf((short)0, (short)1);
			if (res != 32767)
				return 5;
			res = LoopAddOvf((short)0, (short)-1);
			if (res != 32768)
				return 6;

			res = LoopAddOvf((int)int.MaxValue - 1234, 1);
			if (res != 1234)
				return 7;
			res = LoopAddOvf((int)int.MinValue + 456, -1);
			if (res != 456)
				return 8;
			res = LoopAddOvf((uint)uint.MaxValue - 1234, 1);
			if (res != 1234)
				return 9;
			res = LoopAddOvf((long)long.MaxValue - 1234, 1);
			if (res != 1234)
				return 10;
			res = LoopAddOvf((long)long.MinValue + 456, -1);
			if (res != 456)
				return 11;
			res = LoopAddOvf((ulong)ulong.MaxValue - 1234, 1);
			if (res != 1234)
				return 12;


			res = LoopSubOvf((byte)120, (byte)1);
			if (res != 120)
				return 21;
			res = LoopSubOvf((byte)0, (byte)1);
			if (res != 0)
				return 22;
			res = LoopSubOvf((ushort)32760, (ushort)1);
			if (res != 32760)
				return 23;
			res = LoopSubOvf((ushort)0, (ushort)1);
			if (res != 0)
				return 24;
			res = LoopSubOvf((short)12345, (short)1);
			if (res != 45113)
				return 25;
			res = LoopSubOvf((short)12345, (short)-1);
			if (res != 20422)
				return 26;

			res = LoopSubOvf((int)int.MinValue + 456, 1);
			if (res != 456)
				return 27;
			res = LoopSubOvf((int)int.MaxValue - 789, -1);
			if (res != 789)
				return 28;
			res = LoopSubOvf((uint)uint.MinValue + 1234, 1);
			if (res != 1234)
				return 29;
			res = LoopSubOvf((long)long.MinValue + 1234, 1);
			if (res != 1234)
				return 30;
			res = LoopSubOvf((long)long.MaxValue - 456, -1);
			if (res != 456)
				return 31;
			res = LoopSubOvf((ulong)ulong.MinValue + 1234, 1);
			if (res != 1234)
				return 32;


			res = LoopMulOvf((byte)1, (byte)2);
			if (res != 7)
				return 41;
			res = LoopMulOvf((byte)1, (byte)3);
			if (res != 5)
				return 42;
			res = LoopMulOvf((ushort)1, (ushort)2);
			if (res != 15)
				return 43;
			res = LoopMulOvf((ushort)1, (ushort)3);
			if (res != 10)
				return 44;
			res = LoopMulOvf((short)1, (short)2);
			if (res != 14)
				return 45;
			res = LoopMulOvf((short)1, (short)-2);
			if (res != 15)
				return 46;

			res = LoopMulOvf((int)1, 2);
			if (res != 30)
				return 47;
			res = LoopMulOvf((int)1, -2);
			if (res != 31)
				return 48;
			res = LoopMulOvf((uint)1, 2);
			if (res != 31)
				return 49;
			res = LoopMulOvf((long)1, -2);
			if (res != 63)
				return 50;
			res = LoopMulOvf((long)1, 2);
			if (res != 62)
				return 51;
			res = LoopMulOvf((ulong)1, 2);
			if (res != 63)
				return 52;


			res = LoopConvInt((long)int.MaxValue - 500, 1);
			if (res != 501)
				return 60;
			res = LoopConvInt((long)int.MinValue + 500, -1);
			if (res != 501)
				return 61;

			res = LoopConvInt((ulong)int.MaxValue - 500, 1);
			if (res != 501)
				return 62;

			res = LoopConvInt((double)int.MaxValue - 500, 0.5);
			if (res != 1002)
				return 63;
			res = LoopConvInt((double)int.MinValue + 500, -0.5);
			if (res != 1002)
				return 64;


			res = LoopConvLong(long.MaxValue - 500, 1);
			if (res != 501)
				return 65;

			res = LoopConvLong((ulong)long.MaxValue - 500, 1);
			if (res != 501)
				return 66;

			res = LoopConvLong((double)long.MaxValue - 9999999, 1000);
			if (res != 9766)
				return 67;
			res = LoopConvLong((double)long.MinValue + 9999999, -5000);
			if (res != 1954)
				return 68;

			res = LoopConvULong(1234, -1);
			if (res != 1235)
				return 69;

			res = LoopConvULong(long.MaxValue - 999, 1);
			if (res != 1000)
				return 70;

			res = LoopConvULong((double)1234, -1);
			if (res != 1235)
				return 71;
			/*res = LoopConvULong((double)ulong.MaxValue - 9999999, 5000);
			if (res != 2442)
				return 72;*/

			return 0;
		}

		public static int Entry()
		{
			int res = TestCkfinite();
			if (res != 0)
				return res;

			if (!float.IsNaN(float.NaN))
				return 10;

			if (!double.IsNaN(double.NaN))
				return 11;

			if (!float.IsPositiveInfinity(float.PositiveInfinity))
				return 12;

			if (!double.IsPositiveInfinity(double.PositiveInfinity))
				return 13;

			if (!float.IsNegativeInfinity(float.NegativeInfinity))
				return 14;

			if (!double.IsNegativeInfinity(double.NegativeInfinity))
				return 15;

			res = TestOverflow();
			if (res != 0)
				return -res;

			res = OVFTestAdd.Entry();
			if (res != 0)
				return 30 + res;

			res = OVFTestSub.Entry();
			if (res != 0)
				return 40 + res;

			res = OVFTestMul.Entry();
			if (res != 0)
				return 50 + res;

			res = OVFTestDiv.Entry();
			if (res != 0)
				return 60 + res;

			res = OVFTest2Add.Entry();
			if (res != 0)
				return 70 + res;

			res = OVFTest2Sub.Entry();
			if (res != 0)
				return 80 + res;

			res = OVFTest2Mul.Entry();
			if (res != 0)
				return 90 + res;

			res = OVFTest2Div.Entry();
			if (res != 0)
				return 100 + res;

			res = OVFTest3Add.Entry();
			if (res != 0)
				return 110 + res;

			res = OVFTest3Sub.Entry();
			if (res != 0)
				return 120 + res;

			res = OVFTest3Mul.Entry();
			if (res != 0)
				return 130 + res;

			res = OVFTest3Div.Entry();
			if (res != 0)
				return 140 + res;

			return 0;
		}
	}
}
