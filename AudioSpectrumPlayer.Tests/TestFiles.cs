namespace AudioSpectrumPlayer.Tests
{
	internal static class TestFiles
	{
		public static string Get(string fileName)
		{
			string path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
			if (!File.Exists(path))
			{
				throw new FileNotFoundException(
					$"Test file '{fileName}' not found at '{path}'. Run scripts/generate-test-audio.sh "
					+ "(needs ffmpeg) to generate the test files, then rebuild.",
					path);
			}
			return path;
		}
	}
}
