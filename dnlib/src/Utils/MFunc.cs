// dnlib: See LICENSE.txt for more info

﻿namespace dnlib.Utils {
	delegate T MFunc<out T>();
	delegate U MFunc<in T, out U>(T t);

	/// <summary>
	/// Same as Func delegate
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="U"></typeparam>
	/// <typeparam name="V"></typeparam>
	/// <param name="t"></param>
	/// <param name="u"></param>
	/// <returns></returns>
	public delegate V MFunc<in T, in U, out V>(T t, U u);
}
