namespace TimeTxt.Core
{
	public interface ILogWriter
	{
		void WriteLine();
		void WriteLine(string message);
		void WriteLine(string message, object arg0);
		void WriteLine(string message, object arg0, object arg1);
		void WriteLine(string message, object arg0, object arg1, object arg2);
		void WriteLine(string message, params object[] args);
	}
}
