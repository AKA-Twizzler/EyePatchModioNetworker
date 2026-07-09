namespace ModioModNetworker.UI;

public class KeyboardManager
{
	public static string typed = "";

	public static void Append(string character)
	{
		typed += character;
	}

	public static void Backspace()
	{
		if (typed.Length > 0)
		{
			typed = typed.Substring(0, typed.Length - 1);
		}
	}
}
