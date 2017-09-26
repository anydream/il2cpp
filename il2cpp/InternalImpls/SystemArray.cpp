int32_t met_lPpD91_Array__get_Rank(struct cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return 1;
	return ary->Rank;
}
int32_t met_K3nLe2_Array__get_Length(struct cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return ((int32_t*)&ary[1])[0];
	else
	{
		int32_t length = 1;
		for (int32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((int32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}
int64_t met_1VfpJ1_Array__get_LongLength(struct cls_System_Array* ary)
{
	if (ary->Rank == 0)
		return ((int32_t*)&ary[1])[0];
	else
	{
		int64_t length = 1;
		for (int32_t i = 0, sz = ary->Rank; i < sz; ++i)
			length *= ((int32_t*)&ary[1])[i * 2 + 1];
		return length;
	}
}
int32_t met_atlU34_Array__GetLength(struct cls_System_Array* ary, int32_t dim)
{
	if (ary->Rank == 0)
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return ((int32_t*)&ary[1])[0];
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((int32_t*)&ary[1])[dim * 2 + 1];
	}
}
int32_t met_Lg9HN_Array__GetLowerBound(struct cls_System_Array* ary, int32_t dim)
{
	if (ary->Rank == 0)
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return 0;
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((int32_t*)&ary[1])[dim * 2];
	}
}
int32_t met_76bue2_Array__GetUpperBound(struct cls_System_Array* ary, int32_t dim)
{
	if (ary->Rank == 0)
	{
		IL2CPP_CHECK_RANGE(0, 1, dim);
		return ((int32_t*)&ary[1])[0] - 1;
	}
	else
	{
		IL2CPP_CHECK_RANGE(0, ary->Rank, dim);
		return ((int32_t*)&ary[1])[dim * 2] + ((int32_t*)&ary[1])[dim * 2 + 1] - 1;
	}
}