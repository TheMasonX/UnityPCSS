using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowRangeAttribute : PropertyAttribute
{
	public readonly int min;
	public readonly int max;
	public readonly int pow;
	public readonly bool linear;

	public PowRangeAttribute (int min, int max, int pow = 2, bool linear = true)
	{
		this.pow = pow;
		this.linear = linear;

		this.min = RoundToPow(min);
		this.max = RoundToPow(max);
	}

	public int RoundToPow (int value)
	{
		int exp = GetExp(value);
		if (exp < 1)
			return 0;
		int output = Mathf.RoundToInt(Mathf.Pow(pow, exp));

		return output;
	}

	public int GetExp (int value)
	{
		if (value < 1)
			return 0;
		float exp = Mathf.Log(value, pow);

		return Mathf.RoundToInt(exp);
	}
}
