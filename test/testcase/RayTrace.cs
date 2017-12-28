using System;

namespace testcase
{
	/*static class TestRayTrace
	{
		static double MathSqrt(double n)
		{
			return Math.Sqrt(n);
		}
		static double MathAbs(double n)
		{
			return Math.Abs(n);
		}
		static double MathSin(double n)
		{
			return Math.Sin(n);
		}
		static double MathCos(double n)
		{
			return Math.Cos(n);
		}
		static double MathPow(double n, double m)
		{
			return Math.Pow(n, m);
		}
		static double MathMax(double v1, double v2)
		{
			return v1 > v2 ? v1 : v2;
		}

		private static double MathPI = 3.14159265358979;

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

		public struct Vec
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
				mul(out this, ref this, 1 / MathSqrt(x * x + y * y + z * z));
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
				maxC = MathMax(MathMax(c.x, c.y), c.z);
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
					double dets = MathSqrt(det);

					if (b - dets > eps)
						return b - dets;
					else if (b + dets > eps)
						return b + dets;
					else
						return 1e20;
				}
			}
		};

		public class Smallpt
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

			public static int toInt(double x)
			{
				return (int)(MathPow(clamp(x), 1 / 2.2) * 255 + .5);
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
							double r1 = 2 * MathPI * rand();
							double r2 = rand();
							double r2s = MathSqrt(r2);

							Vec w = nl;
							Vec wo = MathAbs(w.x) > .1 ? Vec.YAxis : Vec.XAxis;
							//Vec u = (wo % w).norm();
							Vec u;
							Vec.cross(out u, ref wo, ref w);
							u.normal();
							//Vec v = w % u;
							Vec v;
							Vec.cross(out v, ref w, ref u);

							//Vec d = (u * (MathCos(r1) * r2s) + v * (MathSin(r1) * r2s) + w * MathSqrt(1 - r2)).norm();
							Vec d, ta, tb;
							Vec.mul(out d, ref u, MathCos(r1) * r2s);
							Vec.mul(out ta, ref v, MathSin(r1) * r2s);
							Vec.mul(out tb, ref w, MathSqrt(1 - r2));
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
								//Vec tdir = (r.d * nnt - n * ((into ? 1 : -1) * (ddn * nnt + MathSqrt(cos2t)))).norm();
								double temp = ddn * nnt + MathSqrt(cos2t);
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

			private static Vec[] RenderEntry()
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
									double dx = r1 < 1 ? MathSqrt(r1) - 1 : 1 - MathSqrt(2 - r1);
									double dy = r2 < 1 ? MathSqrt(r2) - 1 : 1 - MathSqrt(2 - r2);
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
				return c;
			}
		}
	}
	*/

	[CodeGen]
	static class TestRayTrace2
	{
		static double MathSqrt(double n)
		{
			return Math.Sqrt(n);
		}
		static double MathAbs(double n)
		{
			return Math.Abs(n);
		}
		static double MathSin(double n)
		{
			return Math.Sin(n);
		}
		static double MathCos(double n)
		{
			return Math.Cos(n);
		}
		static double MathPow(double n, double m)
		{
			return Math.Pow(n, m);
		}

		public static double clamp(double x)
		{
			if (x < 0)
				return 0;
			else if (x > 1)
				return 1;
			else
				return x;
		}

		public static int toInt(double x)
		{
			return (int)(Math.Pow(clamp(x), 1 / 2.2) * 255 + .5);
		}

		static double M_PI = 3.141592653589793238462643;

		public struct RandomLCG
		{
			uint mSeed;

			public RandomLCG(uint seed = 0)
			{
				mSeed = seed;
			}

			public double NextNumber()
			{
				mSeed = 214013u * mSeed + 2531011u;
				return mSeed * (1.0 / 4294967296.0);
			}
		};

		public struct Vec
		{
			public double x, y, z;

			public Vec(double px, double py, double pz)
			{
				x = px;
				y = py;
				z = pz;
			}

			public Vec Add(ref Vec b)
			{
				return new Vec(x + b.x, y + b.y, z + b.z);
			}

			public Vec Sub(ref Vec b)
			{
				return new Vec(x - b.x, y - b.y, z - b.z);
			}

			public Vec Mul(double b)
			{
				return new Vec(x * b, y * b, z * b);
			}

			public Vec Mul(ref Vec b)
			{
				return new Vec(x * b.x, y * b.y, z * b.z);
			}

			public Vec Norm()
			{
				return this.Mul(1.0 / MathSqrt(x * x + y * y + z * z));
			}

			public double Dot(ref Vec b)
			{ // dot
				return x * b.x + y * b.y + z * b.z;
			}

			public Vec Cross(ref Vec b)
			{ // cross
				return new Vec(y * b.z - z * b.y, z * b.x - x * b.z, x * b.y - y * b.x);
			}
		};

		static readonly Vec Vec_Zero = new Vec(0, 0, 0);
		static readonly Vec Vec_XAxis = new Vec(1, 0, 0);
		static readonly Vec Vec_YAxis = new Vec(0, 1, 0);
		static readonly Vec Vec_ZAxis = new Vec(0, 0, 1);

		public struct Ray
		{
			public Vec o, d;

			public Ray(ref Vec po, ref Vec pd)
			{
				o = po;
				d = pd;
			}
		};

		// material types, used in radiance()
		public enum MatType
		{
			DIFF,
			SPEC,
			REFR
		};

		public struct Sphere
		{
			public Vec p, e, c;      // position, emission, color
			public Vec cc;
			public double rad;       // radius
			public double sqRad;
			public double maxC;
			public MatType refl;      // reflection type (DIFFuse, SPECular, REFRactive)

			public Sphere(double prad, ref Vec pp, ref Vec pe, ref Vec pc, MatType prefl)
			{
				p = pp;
				e = pe;
				c = pc;
				rad = prad;
				refl = prefl;

				sqRad = rad * rad;
				maxC = c.x > c.y && c.y > c.z ? c.x : c.y > c.z ? c.y : c.z;
				cc = c.Mul(1.0 / maxC);
			}

			public Sphere(double prad, Vec pp, Vec pe, Vec pc, MatType prefl)
			{
				p = pp;
				e = pe;
				c = pc;
				rad = prad;
				refl = prefl;

				sqRad = rad * rad;
				maxC = c.x > c.y && c.y > c.z ? c.x : c.y > c.z ? c.y : c.z;
				cc = c.Mul(1.0 / maxC);
			}

			// returns distance, 1e20 if nohit
			public double intersect(ref Ray r)
			{
				// Solve t^2*d.d + 2*t*(o-p).d + (o-p).(o-p)-R^2 = 0
				Vec op = p.Sub(ref r.o);
				double b = op.Dot(ref r.d);
				double det = b * b - op.Dot(ref op) + sqRad;
				double eps = 1e-4;

				if (det < 0)
					return 1e20;
				else
				{
					double dets = MathSqrt(det);

					if (b - dets > eps)
						return b - dets;
					else if (b + dets > eps)
						return b + dets;
					else
						return 1e20;
				}
			}
		};

		//Scene: radius, position, emission, color, material
		static Sphere[] spheres =
		{
			new Sphere(1e5,  new Vec(1e5 + 1,40.8,81.6),   Vec_Zero, new Vec(.75,.25,.25), MatType.DIFF),//Left
			new Sphere(1e5,  new Vec(-1e5 + 99,40.8,81.6), Vec_Zero, new Vec(.25,.25,.75), MatType.DIFF),//Rght
			new Sphere(1e5,  new Vec(50,40.8, 1e5),        Vec_Zero, new Vec(.75,.75,.75), MatType.DIFF),//Back
			new Sphere(1e5,  new Vec(50,40.8,-1e5 + 170),  Vec_Zero, Vec_Zero,             MatType.DIFF),//Frnt
			new Sphere(1e5,  new Vec(50, 1e5, 81.6),       Vec_Zero, new Vec(.75,.75,.75), MatType.DIFF),//Botm
			new Sphere(1e5,  new Vec(50,-1e5 + 81.6,81.6), Vec_Zero, new Vec(.75,.75,.75), MatType.DIFF),//Top
			new Sphere(16.5, new Vec(27,16.5,47),          Vec_Zero, new Vec(1,1,1).Mul(.999),  MatType.SPEC),//Mirr
			new Sphere(16.5, new Vec(73,16.5,78),          Vec_Zero, new Vec(1,1,1).Mul(.999),  MatType.REFR),//Glas
			new Sphere(600,  new Vec(50,681.6 - .27,81.6), new Vec(12,12,12), Vec_Zero,         MatType.DIFF) //Lite
		};

		public static unsafe Sphere* intersect(ref Ray r, out double t)
		{
			t = 1e20;
			Sphere* ret = null;

			fixed (Sphere* pStart = &spheres[0])
			{
				int sz = spheres.Length;
				for (Sphere* s = pStart; s != pStart + sz; ++s)
				{
					double d = s->intersect(ref r);
					if (d < t)
					{
						t = d;
						ret = s;
					}
				}
			}
			return ret;
		}

		public static unsafe Vec radiance(ref Ray r, int depth, ref RandomLCG rand)
		{
			double t;                               // distance to intersection
			Sphere* obj = intersect(ref r, out t);

			if (obj == null)
				return Vec_Zero; // if miss, return black
			else
			{
				int newDepth = depth + 1;
				bool isMaxDepth = newDepth > 100;

				// Russian roulette for path termination
				bool isUseRR = newDepth > 5;
				bool isRR = isUseRR && rand.NextNumber() < obj->maxC;

				if (isMaxDepth || (isUseRR && !isRR))
					return obj->e;
				else
				{
					Vec f = (isUseRR && isRR) ? obj->cc : obj->c;
					var tmp1 = r.d.Mul(t);
					Vec x = r.o.Add(ref tmp1);
					Vec n = (x.Sub(ref obj->p)).Norm();
					Vec nl = n.Dot(ref r.d) < 0 ? n : n.Mul(-1);

					if (obj->refl == MatType.DIFF)
					{ // Ideal DIFFUSE reflection
						double r1 = 2 * M_PI * rand.NextNumber();
						double r2 = rand.NextNumber();
						double r2s = MathSqrt(r2);

						Vec w = nl;
						Vec wo = w.x < -0.1 || w.x > 0.1 ? Vec_YAxis : Vec_XAxis;
						Vec u = (wo.Cross(ref w)).Norm();
						Vec v = w.Cross(ref u);

						var tmp2 = v.Mul(MathSin(r1)).Mul(r2s);
						var tmp3 = w.Mul(MathSqrt(1 - r2));
						Vec d = (u.Mul(MathCos(r1)).Mul(r2s).Add(ref tmp2).Add(ref tmp3)).Norm();

						var tmp4 = new Ray(ref x, ref d);
						var tmp5 = radiance(ref tmp4, newDepth, ref rand);
						var tmp6 = f.Mul(ref tmp5);
						return obj->e.Add(ref tmp6);
					}
					else if (obj->refl == MatType.SPEC) // Ideal SPECULAR reflection
					{
						var tmp8 = n.Mul(2 * (n.Dot(ref r.d)));
						var tmp9 = r.d.Sub(ref tmp8);
						var tmp7 = new Ray(ref x, ref tmp9);
						var tmp10 = radiance(ref tmp7, newDepth, ref rand);
						var tmp11 = f.Mul(ref tmp10);
						return obj->e.Add(ref tmp11);
					}
					else
					{ // Ideal dielectric REFRACTION
						var tmp100 = n.Mul(2 * (n.Dot(ref r.d)));
						var tmp101 = r.d.Sub(ref tmp100);
						Ray reflRay = new Ray(ref x, ref tmp101);
						bool into = n.Dot(ref nl) > 0;  // Ray from outside going in?
						double nc = 1;
						double nt = 1.5;
						double nnt = into ? nc / nt : nt / nc;
						double ddn = r.d.Dot(ref nl);
						double cos2t = 1 - nnt * nnt * (1 - ddn * ddn);

						if (cos2t < 0) // Total internal reflection
						{
							var tmp12 = radiance(ref reflRay, newDepth, ref rand);
							var tmp13 = f.Mul(ref tmp12);
							return obj->e.Add(ref tmp13);
						}
						else
						{
							var tmp14 = n.Mul((into ? 1 : -1) * (ddn * nnt + MathSqrt(cos2t)));
							Vec tdir = (r.d.Mul(nnt).Sub(ref tmp14)).Norm();
							double a = nt - nc;
							double b = nt + nc;
							double R0 = (a * a) / (b * b);
							double c = 1 - (into ? -ddn : tdir.Dot(ref n));
							double Re = R0 + (1 - R0) * c * c * c * c * c;
							double Tr = 1 - Re;
							double P = .25 + .5 * Re;
							double RP = Re / P;
							double TP = Tr / (1 - P);

							Vec result;
							if (newDepth > 2)
							{
								// Russian roulette and splitting for selecting reflection and/or refraction
								if (rand.NextNumber() < P)
									result = radiance(ref reflRay, newDepth, ref rand).Mul(RP);
								else
								{
									var tmp15 = new Ray(ref x, ref tdir);
									result = radiance(ref tmp15, newDepth, ref rand).Mul(TP);
								}
							}
							else
							{
								var tmp16 = new Ray(ref x, ref tdir);
								var tmp17 = radiance(ref tmp16, newDepth, ref rand).Mul(Tr);
								result = radiance(ref reflRay, newDepth, ref rand).Mul(Re).Add(ref tmp17);
							}

							var tmp18 = f.Mul(ref result);
							return obj->e.Add(ref tmp18);
						}
					}
				}
			}
		}

		public static Vec[] RenderEntry()
		{
			int w = 256;
			int h = 256;
			int samps = 25; // # samples

			var tmp1 = new Vec(50, 52, 295.6);
			var tmp2 = new Vec(0, -0.042612, -1).Norm();
			Ray cam = new Ray(ref tmp1, ref tmp2); // cam pos, dir
			Vec cx = new Vec(w * .5135 / h, 0, 0);
			Vec cy = (cx.Cross(ref cam.d)).Norm().Mul(.5135);
			Vec[] c = new Vec[w * h];

			//#pragma omp parallel for schedule(dynamic, 1)       // OpenMP
			// Loop over image rows
			for (int y = 0; y < h; y++)
			{
				//fprintf(stderr,"\rRendering (%d spp) %5.2f%%",samps*4,100.*y/(h-1));
				RandomLCG rand = new RandomLCG((uint)y);

				// Loop cols
				for (ushort x = 0; x < w; x++)
				{
					// 2x2 subpixel rows
					for (int sy = 0; sy < 2; sy++)
					{
						int i = (h - y - 1) * w + x;

						// 2x2 subpixel cols
						for (int sx = 0; sx < 2; sx++)
						{
							Vec r = Vec_Zero;
							for (int s = 0; s < samps; s++)
							{
								double r1 = 2 * rand.NextNumber();
								double r2 = 2 * rand.NextNumber();
								double dx = r1 < 1 ? MathSqrt(r1) - 1 : 1 - MathSqrt(2 - r1);
								double dy = r2 < 1 ? MathSqrt(r2) - 1 : 1 - MathSqrt(2 - r2);

								var tmp3 = cy.Mul(((sy + .5 + dy) / 2 + y) / h - .5);
								Vec d = cx.Mul(((sx + .5 + dx) / 2 + x) / w - .5).Add(
									ref tmp3).Add(ref cam.d);

								var tmp4 = d.Mul(140);
								var tmp5 = cam.o.Add(ref tmp4);
								var tmp6 = d.Norm();
								var tmp7 = new Ray(ref tmp5, ref tmp6);
								var tmp8 = radiance(ref tmp7, 0, ref rand).Mul(1.0 / samps);
								r = r.Add(ref tmp8);
							}
							var tmp9 = new Vec(clamp(r.x), clamp(r.y), clamp(r.z)).Mul(.25);
							c[i] = c[i].Add(ref tmp9);
						}
					}
				}
			}
			return c;
		}

		public static uint CalcHash(Vec v)
		{
			uint x = (uint)toInt(v.x);
			uint y = (uint)toInt(v.y);
			uint z = (uint)toInt(v.z);

			uint hash = x;
			hash += hash << 5;
			hash ^= y;
			hash += hash << 5;
			hash ^= z;

			return hash;
		}

		public static int Entry()
		{
			var result = RenderEntry();
			uint hash = 0;
			foreach (var item in result)
			{
				hash += hash << 5;
				hash ^= CalcHash(item);
			}
			if (hash != 2449431406)
				return 1;
			return 0;
		}
	}
}
