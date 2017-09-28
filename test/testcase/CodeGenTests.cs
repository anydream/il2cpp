using System;
using System.Diagnostics;

namespace testcase
{
	class CodeGenAttribute : Attribute
	{
	}

	static class Helper
	{
		public static bool IsEquals(this float lhs, float rhs, float prec = 0.00001f)
		{
			float abs = lhs - rhs;
			if (abs < 0)
				abs = -abs;
			return abs < prec;
		}

		public static bool IsEquals(this double lhs, double rhs, double prec = 0.00001)
		{
			double abs = lhs - rhs;
			if (abs < 0)
				abs = -abs;
			return abs < prec;
		}
	}

	static class HelloWorld
	{
		public static int Entry()
		{
			return 42;
		}
	}

	[CodeGen]
	static class Fibonacci
	{
		static long Fib(int n)
		{
			if (n < 2)
				return n;
			else
				return Fib(n - 1) + Fib(n - 2);
		}

		public static long Entry()
		{
			return Fib(43);
		}
	}

	[CodeGen]
	static class Fibonacci2
	{
		static long Fib(int n)
		{
			long a = 1;
			long b = 1;
			for (int i = 0; i < n - 1; ++i)
			{
				long t = a;
				a = b;
				b = t + b;
			}
			return a;
		}

		public static long Entry()
		{
			return Fib(43);
		}
	}

	[CodeGen]
	static class TestCallVirt
	{
		interface Inf
		{
			int Foo();
		}

		class ClsA : Inf
		{
			public int Foo()
			{
				return 123;
			}
		}

		class ClsB : Inf
		{
			public int Foo()
			{
				return 456;
			}
		}

		private static int Bla(Inf inf)
		{
			return inf.Foo();
		}

		public static int Entry()
		{
			if (Bla(new ClsB()) - Bla(new ClsA()) != 333)
				return 1;
			return 0;
		}
	}

	[CodeGen]
	static class TestValueType
	{
		private static short sfldI2;

		struct MyStru
		{
			public int fldI4;
			public double fldR8;
		}

		class MyCls
		{
			public int fld;
		}

		struct MyStruHasRef
		{
			public long fldI8;
			public MyCls cls;
		}

		private static MyStru Foo1(MyStru s, ref MyStru rs, out MyStru os)
		{
			rs = s;
			os = new MyStru { fldI4 = 123, fldR8 = 3.1415926 };
			return os;
		}

		private static MyStruHasRef Foo2(MyStruHasRef s, ref MyStruHasRef rs, out MyStruHasRef os)
		{
			rs = new MyStruHasRef { fldI8 = ~1, cls = new MyCls { fld = 42 } };
			os = rs;
			rs.fldI8 -= s.fldI8;
			return rs;
		}

		public static int Entry()
		{
			sfldI2 = 26;
			MyStru rs = new MyStru();
			var ret = Foo1(new MyStru { fldI4 = 10, fldR8 = 1.44 }, ref rs, out var os);

			if (ret.fldI4 != 123)
				return 1;
			if (ret.fldR8 != 3.1415926)
				return 2;
			if (ret.fldI4 != os.fldI4)
				return 3;
			if (ret.fldR8 != os.fldR8)
				return 4;
			if (rs.fldI4 != 10)
				return 5;
			if (rs.fldR8 != 1.44)
				return 6;

			MyStruHasRef rs2 = new MyStruHasRef();
			var ret2 = Foo2(new MyStruHasRef { fldI8 = 999999 }, ref rs2, out var os2);

			if (ret2.cls.fld != 42)
				return 7;
			if (ret2.fldI8 != -1000001)
				return 8;
			if (rs2.cls.fld != os2.cls.fld)
				return 9;
			if (rs2.cls.fld != ret2.cls.fld)
				return 10;
			if (ret2.fldI8 != rs2.fldI8)
				return 11;
			if (os2.fldI8 != -2)
				return 12;

			if (sfldI2 != 26)
				return 13;

			return 0;
		}
	}

	[CodeGen]
	static class TestSZArray
	{
		public static int Entry()
		{
			float[] fary = new float[10];

			var rank = fary.Rank;
			var len = fary.Length;
			var llen = fary.LongLength;
			var len2 = fary.GetLength(0);
			var llen2 = fary.GetLongLength(0);
			var lb = fary.GetLowerBound(0);
			var ub = fary.GetUpperBound(0);

			if (rank != 1)
				return 1;
			if (len != len2)
				return 2;
			if (llen != llen2)
				return 3;
			if (len != llen)
				return 4;
			if (lb != 0)
				return 5;
			if (ub != 9)
				return 6;

			fary[0] = 1.1f;
			fary[3] = 3.3f;
			fary[5] = 5.5f;

			if (fary[0] != 1.1f ||
				fary[3] != 3.3f ||
				fary[5] != 5.5f)
				return 7;

			float sum = 0;
			foreach (float n in fary)
				sum += n;

			if (!sum.IsEquals(9.9f))
				return 8;

			ushort[] usary = new ushort[5];

			rank = usary.Rank;
			len = usary.Length;
			llen = usary.LongLength;
			len2 = usary.GetLength(0);
			llen2 = usary.GetLongLength(0);
			lb = usary.GetLowerBound(0);
			ub = usary.GetUpperBound(0);

			if (rank != 1)
				return 9;
			if (len != len2)
				return 10;
			if (llen != llen2)
				return 11;
			if (len != llen)
				return 12;
			if (lb != 0)
				return 13;
			if (ub != 4)
				return 14;

			usary[1] = 42;
			usary[3] = 0xFFFF;

			if (usary[1] != 42 ||
				usary[3] != 65535)
				return 15;

			foreach (ushort n in usary)
				sum += n;

			if (!sum.IsEquals(65586.9f))
				return 16;

			return 0;
		}
	}

	[CodeGen]
	static class TestSZArrayPerf
	{
		public static long Entry(int times)
		{
			long sum = 0;
			for (int i = 0; i < times; ++i)
				sum += TestSZArray.Entry();
			return sum;
		}
	}

	[CodeGen]
	static class TestMDArray
	{
		public static int Entry()
		{
			float[,] fary = new float[2, 3];

			var rank = fary.Rank;
			var len = fary.Length;
			var llen = fary.LongLength;
			var len0 = fary.GetLength(0);
			var len1 = fary.GetLength(1);
			var llen0 = fary.GetLongLength(0);
			var llen1 = fary.GetLongLength(1);
			var lb0 = fary.GetLowerBound(0);
			var ub0 = fary.GetUpperBound(0);
			var lb1 = fary.GetLowerBound(1);
			var ub1 = fary.GetUpperBound(1);

			if (rank != 2)
				return 1;
			if (len != llen)
				return 2;
			if (len0 != 2)
				return 3;
			if (len1 != 3)
				return 4;
			if (llen0 != 2)
				return 5;
			if (llen1 != 3)
				return 6;
			if (lb0 != 0)
				return 7;
			if (ub0 != 1)
				return 8;
			if (lb1 != 0)
				return 9;
			if (ub1 != 2)
				return 10;
			if (len != 6)
				return 11;

			fary[0, 0] = 123.1f;
			fary[1, 0] = 456.2f;
			fary[1, 2] = 789.3f;

			if (fary[0, 0] != 123.1f)
				return 12;
			if (fary[1, 0] != 456.2f)
				return 13;
			if (fary[1, 2] != 789.3f)
				return 14;

			float sum = 0;
			foreach (float n in fary)
				sum += n;

			if (!sum.IsEquals(1368.6f))
				return 15;

			short[,,] sary3d = new short[2, 3, 4];
			/*{
				{
					{ 1, 2, 3, 4 }, { 5, 6, 7, 8 }, { 9, 10, 11, 12 }
				},
				{
					{ 13, 14, 15, 16 }, { 17, 18, 19, 20 }, { 21, 22, 23, 24 }
				}
			};*/

			rank = sary3d.Rank;
			len = sary3d.Length;
			llen = sary3d.LongLength;
			len0 = sary3d.GetLength(0);
			len1 = sary3d.GetLength(1);
			var len2 = sary3d.GetLength(2);
			llen0 = sary3d.GetLongLength(0);
			llen1 = sary3d.GetLongLength(1);
			var llen2 = sary3d.GetLongLength(2);
			lb0 = sary3d.GetLowerBound(0);
			ub0 = sary3d.GetUpperBound(0);
			lb1 = sary3d.GetLowerBound(1);
			ub1 = sary3d.GetUpperBound(1);
			var lb2 = sary3d.GetLowerBound(2);
			var ub2 = sary3d.GetUpperBound(2);

			if (rank != 3)
				return 16;
			if (len != llen)
				return 17;
			if (len != 24)
				return 18;
			if (len0 != 2)
				return 19;
			if (len1 != 3)
				return 20;
			if (len2 != 4)
				return 21;
			if (llen0 != 2)
				return 22;
			if (llen1 != 3)
				return 23;
			if (llen2 != 4)
				return 24;
			if (lb0 != 0)
				return 25;
			if (ub0 != 1)
				return 26;
			if (lb1 != 0)
				return 27;
			if (ub1 != 2)
				return 28;
			if (lb2 != 0)
				return 29;
			if (ub2 != 3)
				return 30;

			short num = 0;
			for (int x = 0; x < 2; ++x)
			{
				for (int y = 0; y < 3; ++y)
				{
					for (int z = 0; z < 4; ++z)
					{
						sary3d[x, y, z] = ++num;
					}
				}
			}

			num = 0;
			foreach (short n in sary3d)
			{
				if (n != ++num)
					return 31;
			}

			return 0;
		}
	}

	[CodeGen]
	static class TestMDArrayPerf
	{
		public static long Entry(int times)
		{
			long sum = 0;
			for (int i = 0; i < times; ++i)
				sum += TestMDArray.Entry();
			return sum;
		}
	}


	[CodeGen]
	static class TestValueTypeArray
	{
		struct MyStru
		{
			public int fldI4;
			public double fldR8;
		}

		public static int Entry()
		{
			MyStru[] sary = new MyStru[30];
			for (int i = 0; i < sary.Length; ++i)
			{
				sary[i].fldI4 = i + 1;
				sary[i].fldR8 = i * 100 + 0.1234567;
			}

			for (int i = sary.Length - 1; i >= 0; --i)
			{
				if (sary[i].fldI4 != i + 1)
					return 1;
				if (!sary[i].fldR8.IsEquals(i * 100 + 0.1234567))
					return 2;
			}

			//MyStru[,,] sary3d = new MyStru[5, 4, 3];

			return 0;
		}
	}

	[CodeGen]
	static class TestEnum
	{
		enum MyEnum
		{
			AA,
			BB,
			CC = 123
		}

		enum MyEnumI8 : long
		{
			DD = 3,
			EE
		}

		class MyCls
		{
			public MyEnum enum1;
			public MyEnumI8 enum2;
		}

		static MyEnum senum1 = MyEnum.BB;
		static MyEnumI8 senum2 = MyEnumI8.EE;

		public static int Entry()
		{
			var cls = new MyCls();
			if (cls.enum1 != 0)
				return 1;
			if (cls.enum2 != 0)
				return 2;

			cls.enum1 = senum1;
			cls.enum2 = senum2;

			if (cls.enum1 != MyEnum.BB)
				return 3;
			if (cls.enum2 != MyEnumI8.EE)
				return 4;

			if (cls.enum1 > (MyEnum)1)
				return 5;
			if (cls.enum2 > (MyEnumI8)4)
				return 6;

			return 0;
		}
	}

	//[CodeGen]
	static class TestRayTrace
	{
		class RandomLCG
		{
			private uint mSeed;

			public RandomLCG(uint seed)
			{
				mSeed = seed;
			}

			public double NextDouble()
			{
				mSeed = 214013u * mSeed + 2531011u;
				return mSeed * (1.0 / 4294967296.0);
			}
		}

		struct Vec
		{
			public double x;
			public double y;
			public double z;

			public static readonly Vec Zero = new Vec(0, 0, 0);
			public static readonly Vec XAxis = new Vec(1, 0, 0);
			public static readonly Vec YAxis = new Vec(0, 1, 0);
			public static readonly Vec ZAxis = new Vec(0, 0, 1);

			public Vec(double x, double y, double z)
			{
				this.x = x;
				this.y = y;
				this.z = z;
			}

			public static void add(out Vec result, ref Vec a, ref Vec b)
			{
				result.x = a.x + b.x;
				result.y = a.y + b.y;
				result.z = a.z + b.z;
			}

			public static void sub(out Vec result, ref Vec a, ref Vec b)
			{
				result.x = a.x - b.x;
				result.y = a.y - b.y;
				result.z = a.z - b.z;
			}

			public static void mul(out Vec result, ref Vec a, double b)
			{
				result.x = a.x * b;
				result.y = a.y * b;
				result.z = a.z * b;
			}

			public static void mul(out Vec result, ref Vec a, ref Vec b)
			{
				result.x = a.x * b.x;
				result.y = a.y * b.y;
				result.z = a.z * b.z;
			}

			public void normal()
			{
				mul(out this, ref this, 1 / Math.Sqrt(x * x + y * y + z * z));
			}

			public double dot(ref Vec b)
			{
				return x * b.x + y * b.y + z * b.z;
			}

			public static void cross(out Vec result, ref Vec a, ref Vec b)
			{
				result.x = a.y * b.z - a.z * b.y;
				result.y = a.z * b.x - a.x * b.z;
				result.z = a.x * b.y - a.y * b.x;
			}
		}

		// material types
		enum Refl_t
		{
			DIFF,
			SPEC,
			REFR
		};

		struct Ray
		{
			public Vec o;
			public Vec d;

			public Ray(ref Vec o, ref Vec d)
			{
				this.o = o;
				this.d = d;
			}
		}

		class Sphere
		{
			public double rad;       // radius
			public Vec p, e, c;      // position, emission, color
			public Refl_t refl;      // reflection type (DIFFuse, SPECular, REFRactive)
			public double maxC;
			public Vec cc;
			private double sqRad;

			public Sphere(double rad, Vec p, Vec e, Vec c, Refl_t refl)
			{
				this.rad = rad;
				this.p = p;
				this.e = e;
				this.c = c;
				this.refl = refl;

				sqRad = rad * rad;
				maxC = Math.Max(Math.Max(c.x, c.y), c.z);
				// cc = c * (1.0 / maxC);
				Vec.mul(out cc, ref c, 1.0 / maxC);
			}

			// returns distance, 1e20 if nohit
			public double intersect(ref Ray r)
			{
				// Solve t^2*d.d + 2*t*(o-p).d + (o-p).(o-p)-R^2 = 0
				//Vec op = p - r.o;
				Vec op;
				Vec.sub(out op, ref p, ref r.o);
				double b = op.dot(ref r.d);
				double det = b * b - op.dot(ref op) + sqRad;
				const double eps = 1e-4;

				if (det < 0)
					return 1e20;
				else
				{
					double dets = Math.Sqrt(det);

					if (b - dets > eps)
						return b - dets;
					else if (b + dets > eps)
						return b + dets;
					else
						return 1e20;
				}
			}
		};

		class Smallpt
		{
			//Scene: radius, position, emission, color, material
			static Sphere[] spheres =
			{
				new Sphere(1e5,  new Vec( 1e5+1,40.8,81.6),  Vec.Zero, new Vec(.75,.25,.25), Refl_t.DIFF),//Left
				new Sphere(1e5,  new Vec(-1e5+99,40.8,81.6), Vec.Zero, new Vec(.25,.25,.75), Refl_t.DIFF),//Rght
				new Sphere(1e5,  new Vec(50,40.8, 1e5),      Vec.Zero, new Vec(.75,.75,.75), Refl_t.DIFF),//Back
				new Sphere(1e5,  new Vec(50,40.8,-1e5+170),  Vec.Zero, Vec.Zero,             Refl_t.DIFF),//Frnt
				new Sphere(1e5,  new Vec(50, 1e5, 81.6),     Vec.Zero, new Vec(.75,.75,.75), Refl_t.DIFF),//Botm
				new Sphere(1e5,  new Vec(50,-1e5+81.6,81.6), Vec.Zero, new Vec(.75,.75,.75), Refl_t.DIFF),//Top
				new Sphere(16.5, new Vec(27,16.5,47),        Vec.Zero, new Vec(.999,.999,.999),  Refl_t.SPEC),//Mirr
				new Sphere(16.5, new Vec(73,16.5,78),        Vec.Zero, new Vec(.999,.999,.999),  Refl_t.REFR),//Glas
				new Sphere(600,  new Vec(50,681.6-.27,81.6), new Vec(12,12,12), Vec.Zero,    Refl_t.DIFF) //Lite
			};

			//static Random random = new Random();
			static RandomLCG random = new RandomLCG(0u);

			static double rand()
			{
				return random.NextDouble();
			}

			static double clamp(double x)
			{
				if (x < 0)
					return 0;
				else if (x > 1)
					return 1;
				else
					return x;
			}

			static int toInt(double x)
			{
				return (int)(Math.Pow(clamp(x), 1 / 2.2) * 255 + .5);
			}

			static Sphere intersect(ref Ray r, out double t)
			{
				double d, inf = t = 1e20;
				Sphere ret = null;

				foreach (Sphere s in spheres)
				{
					d = s.intersect(ref r);
					if (d < t)
					{
						t = d;
						ret = s;
					}
				}

				return ret;
			}

			static void radiance(out Vec rad, ref Ray r, int depth)
			{
				double t;   // distance to intersection
				Sphere obj = intersect(ref r, out t);

				if (obj == null)
					rad = Vec.Zero;       // if miss, return black
				else
				{
					int newDepth = depth + 1;
					bool isMaxDepth = newDepth > 100;

					// Russian roulette for path termination
					bool isUseRR = newDepth > 5;
					bool isRR = isUseRR && rand() < obj.maxC;

					if (isMaxDepth || (isUseRR && !isRR))
						rad = obj.e;
					else
					{
						Vec f = (isUseRR && isRR) ? obj.cc : obj.c;
						//Vec x = r.o + r.d * t;
						Vec x;
						Vec.mul(out x, ref r.d, t);
						Vec.add(out x, ref r.o, ref x);
						//Vec n = (x - obj.p).norm();
						Vec n;
						Vec.sub(out n, ref x, ref obj.p);
						n.normal();

						//Vec nl = n.dot(r.d) < 0 ? n : n * -1;
						Vec nl;
						if (n.dot(ref r.d) < 0)
							nl = n;
						else
							Vec.mul(out nl, ref n, -1);

						if (obj.refl == Refl_t.DIFF) // Ideal DIFFUSE reflection
						{
							double r1 = 2 * Math.PI * rand();
							double r2 = rand();
							double r2s = Math.Sqrt(r2);

							Vec w = nl;
							Vec wo = Math.Abs(w.x) > .1 ? Vec.YAxis : Vec.XAxis;
							//Vec u = (wo % w).norm();
							Vec u;
							Vec.cross(out u, ref wo, ref w);
							u.normal();
							//Vec v = w % u;
							Vec v;
							Vec.cross(out v, ref w, ref u);

							//Vec d = (u * (Math.Cos(r1) * r2s) + v * (Math.Sin(r1) * r2s) + w * Math.Sqrt(1 - r2)).norm();
							Vec d, ta, tb;
							Vec.mul(out d, ref u, Math.Cos(r1) * r2s);
							Vec.mul(out ta, ref v, Math.Sin(r1) * r2s);
							Vec.mul(out tb, ref w, Math.Sqrt(1 - r2));
							Vec.add(out d, ref d, ref ta);
							Vec.add(out d, ref d, ref tb);
							d.normal();

							//return obj.e + f.mult(radiance(new Ray(x, d), newDepth));
							Ray ray = new Ray(ref x, ref d);
							Vec childRad;
							radiance(out childRad, ref ray, newDepth);
							Vec.mul(out childRad, ref f, ref childRad);
							Vec.add(out rad, ref obj.e, ref childRad);
						}
						else if (obj.refl == Refl_t.SPEC) // Ideal SPECULAR reflection
						{
							//return obj.e + f.mult(radiance(new Ray(x, r.d - n * 2 * n.dot(r.d)), newDepth));
							Vec reflect;
							Vec.mul(out reflect, ref n, 2 * n.dot(ref r.d));
							Vec.sub(out reflect, ref r.d, ref reflect);

							Ray ray = new Ray(ref x, ref reflect);
							Vec childRad;
							radiance(out childRad, ref ray, newDepth);
							Vec.mul(out childRad, ref f, ref childRad);
							Vec.add(out rad, ref obj.e, ref childRad);
						}
						else // Ideal dielectric REFRACTION
						{
							//Ray reflRay = new Ray(x, r.d - n * (2 * n.dot(ref r.d)));
							Vec reflect;
							Vec.mul(out reflect, ref n, 2 * n.dot(ref r.d));
							Vec.sub(out reflect, ref r.d, ref reflect);
							Ray reflRay = new Ray(ref x, ref reflect);

							bool into = n.dot(ref nl) > 0;  // Ray from outside going in?
							double nc = 1;
							double nt = 1.5;
							double nnt = into ? nc / nt : nt / nc;
							double ddn = r.d.dot(ref nl);
							double cos2t = 1 - nnt * nnt * (1 - ddn * ddn);

							if (cos2t < 0)  // Total internal reflection
							{
								//return obj.e + f.mult(radiance(reflRay, newDepth));
								Vec childRad;
								radiance(out childRad, ref reflRay, newDepth);
								Vec.mul(out childRad, ref f, ref childRad);
								Vec.add(out rad, ref obj.e, ref childRad);
							}
							else
							{
								//Vec tdir = (r.d * nnt - n * ((into ? 1 : -1) * (ddn * nnt + Math.Sqrt(cos2t)))).norm();
								double temp = ddn * nnt + Math.Sqrt(cos2t);
								if (!into) temp = -temp;
								Vec tn;
								Vec.mul(out tn, ref n, temp);
								Vec tdir;

								Vec.mul(out tdir, ref r.d, nnt);
								Vec.sub(out tdir, ref tdir, ref tn);
								tdir.normal();

								double a = nt - nc;
								double b = nt + nc;
								double R0 = (a * a) / (b * b);
								double c = 1 - (into ? -ddn : tdir.dot(ref n));
								double Re = R0 + (1 - R0) * c * c * c * c * c;
								double Tr = 1 - Re;
								double P = .25 + .5 * Re;
								double RP = Re / P;
								double TP = Tr / (1 - P);

								Vec result;
								if (newDepth > 2)
								{
									// Russian roulette and splitting for selecting reflection and/or refraction
									if (rand() < P)
									{
										//result = radiance(reflRay, newDepth) * RP;
										radiance(out result, ref reflRay, newDepth);
										Vec.mul(out result, ref result, RP);
									}
									else
									{
										//result = radiance(new Ray(x, tdir), newDepth) * TP;
										reflRay = new Ray(ref x, ref tdir);
										radiance(out result, ref reflRay, newDepth);
										Vec.mul(out result, ref result, TP);
									}
								}
								else
								{
									//result = radiance(reflRay, newDepth) * Re + radiance(new Ray(x, tdir), newDepth) * Tr;
									radiance(out result, ref reflRay, newDepth);
									Vec.mul(out result, ref result, Re);
									Vec result1;
									reflRay = new Ray(ref x, ref tdir);
									radiance(out result1, ref reflRay, newDepth);
									Vec.mul(out result1, ref result1, Tr);
									Vec.add(out result, ref result, ref result1);
								}

								//return obj.e + f.mult(result);
								Vec.mul(out rad, ref result, ref f);
								Vec.add(out rad, ref rad, ref obj.e);
							}
						}
					}
				}
			}

			public static void Entry()
			{
				const int w = 256;
				const int h = 256;
				int samps = 25; // # samples

				// cam pos, dir
				//Ray cam = new Ray(new Vec(50, 52, 295.6), new Vec(0, -0.042612, -1).norm());
				Vec rd = new Vec(0, -0.042612, -1);
				rd.normal();
				Vec cpos = new Vec(50, 52, 295.6);
				Ray cam = new Ray(ref cpos, ref rd); // cam pos, dir
				Vec cx = new Vec(w * .5135 / h, 0, 0);
				//Vec cy = (cx % cam.d).norm() * .5135;
				Vec cy;
				Vec.cross(out cy, ref cx, ref cam.d);
				cy.normal();
				Vec.mul(out cy, ref cy, .5135);

				// final color buffer
				Vec[] c = new Vec[w * h];

				// Loop over image rows
				for (int y = 0; y < h; y++)
				{
					//Console.Write("\rRendering ({0} spp) {1:F2}%", samps * 4, 100.0 * y / (h - 1));

					// Loop cols
					for (int x = 0; x < w; x++)
					{
						int i = (h - y - 1) * w + x;
						c[i] = Vec.Zero;

						// 2x2 subpixel rows
						for (int sy = 0; sy < 2; sy++)
						{
							// 2x2 subpixel cols
							for (int sx = 0; sx < 2; sx++)
							{
								Vec r = Vec.Zero;
								for (int s = 0; s < samps; s++)
								{
									double r1 = 2 * rand();
									double r2 = 2 * rand();
									double dx = r1 < 1 ? Math.Sqrt(r1) - 1 : 1 - Math.Sqrt(2 - r1);
									double dy = r2 < 1 ? Math.Sqrt(r2) - 1 : 1 - Math.Sqrt(2 - r2);
									//Vec d = cx * (((sx + .5 + dx) / 2 + x) / w - .5) +
									//        cy * (((sy + .5 + dy) / 2 + y) / h - .5) + cam.d;
									Vec temp;
									Vec.mul(out temp, ref cx, (((sx + .5 + dx) / 2 + x) / w - .5));
									Vec d;
									Vec.mul(out d, ref cy, (((sy + .5 + dy) / 2 + y) / h - .5));
									Vec.add(out d, ref d, ref temp);
									Vec.add(out d, ref d, ref cam.d);

									// Camera rays are pushed forward to start in interior
									//Ray camRay = new Ray(cam.o + d * 140, d.norm());
									Vec td;
									Vec.mul(out td, ref d, 140);
									Vec.add(out td, ref cam.o, ref td);
									d.normal();
									Ray camRay = new Ray(ref td, ref d);

									// Accumuate radiance
									//r = r + radiance(camRay, 0) * (1.0 / samps);
									Vec rad;
									radiance(out rad, ref camRay, 0);
									Vec.mul(out rad, ref rad, 1.0 / samps);
									Vec.add(out r, ref r, ref rad);
								}

								// Convert radiance to color
								//c[i] = c[i] + new Vec(clamp(r.x), clamp(r.y), clamp(r.z)) * .25;
								Vec color = new Vec(clamp(r.x), clamp(r.y), clamp(r.z));
								Vec.mul(out color, ref color, .25);
								Vec.add(out c[i], ref c[i], ref color);
							}
						}
					}
				}
			}
		}

		public static void Entry()
		{
			Smallpt.Entry();
		}
	}

	internal class Program
	{
		private static void Main()
		{
			//Console.Write("Input Times: ");
			//int times = int.Parse(Console.ReadLine());
			//Console.WriteLine("Times: {0}", times);
			/*var sw = new Stopwatch();
			sw.Start();
			long res = Fibonacci.Entry();
			sw.Stop();

			Console.WriteLine("Result: {0}, Elapsed: {1}ms", res, sw.ElapsedMilliseconds);*/
		}
	}
}
