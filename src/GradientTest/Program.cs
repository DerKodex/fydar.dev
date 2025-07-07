using SixLabors.ImageSharp;
using SixLabors.ImageSharp.ColorSpaces;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace GradientTest;

internal class Program
{
	private static void Main(string[] args)
	{
		Console.WriteLine("Hello, World!");

		RadialGradientGenerator.GenerateRadialGradientsCss("images/astralswarm.webp");
		RadialGradientGenerator.GenerateRadialGradientsCss("images/asylum.jpg");
		RadialGradientGenerator.GenerateRadialGradientsCss("images/over.png");
		RadialGradientGenerator.GenerateRadialGradientsCss("images/test.png");
		RadialGradientGenerator.GenerateRadialGradientsCss("images/lilypad.png");
		RadialGradientGenerator.GenerateRadialGradientsCss("images/test.jpg");

		Console.ReadLine();
	}
}

public class RadialGradientGenerator
{
	private const int PopulationSize = 32;
	private const int MaxGenerations = 64;
	private const double MutationRate = 0.1;
	private const int ElitismCount = 4;
	private static readonly Random Rng = new();

	private class GradientGene
	{
		public PointF Position { get; set; }
		public Rgb Color { get; set; }
		public float Radius { get; set; }

		public GradientGene Clone()
		{
			return new GradientGene
			{
				Position = Position,
				Color = Color,
				Radius = Radius
			};
		}
	}

	private class GradientChromosome
	{
		public Rgb BackgroundColor { get; set; }
		public List<GradientGene> Genes { get; set; }
		public float Fitness { get; set; } = float.MaxValue;

		public GradientChromosome(
			Rgb backgroundColor,
			int numberOfGenes)
		{
			BackgroundColor = backgroundColor;
			Genes = new List<GradientGene>(numberOfGenes);
		}

		public GradientChromosome Clone()
		{
			return new GradientChromosome(BackgroundColor, Genes.Count)
			{
				Genes = Genes.Select(g => g.Clone()).ToList(),
				Fitness = Fitness
			};
		}
	}

	public static string GenerateRadialGradientsCss(
		string imagePath,
		int numberOfGradients = 5)
	{
		Console.WriteLine();
		Console.WriteLine(imagePath);

		using var originalImage = Image.Load<Rgba32>(imagePath);
		originalImage.Mutate(x => x.Resize(new ResizeOptions
		{
			Size = new Size(64, 64),
			Mode = ResizeMode.Stretch,
			// Sampler = new BicubicResampler()
		}));

		originalImage.SaveAsPng(imagePath + ".small.png");
		originalImage.SaveAsPng(imagePath + ".smallblur.png");

		var dominantColors = GetDominantColors(originalImage, numberOfGradients);

		var population = InitializePopulation(numberOfGradients, dominantColors);

		float imageDiagonal = MathF.Sqrt((originalImage.Width * originalImage.Width) + (originalImage.Height * originalImage.Height));

		for (int i = 0; i < MaxGenerations; i++)
		{
			CalculatePopulationFitness(population, originalImage, imageDiagonal);

			population = CreateNextGeneration(population);
		}

		CalculatePopulationFitness(population, originalImage, imageDiagonal);

		var bestSolution = population.First();

		string output = ConvertToCss(bestSolution);
		Console.WriteLine(output);

		return output;
	}

	private static List<GradientChromosome> InitializePopulation(
		int numberOfGradients,
		List<Rgb> availableColors)
	{

		var population = new List<GradientChromosome>(PopulationSize);
		for (int i = 0; i < PopulationSize; i++)
		{
			var backgroundColor = availableColors[Rng.Next(availableColors.Count)];

			var chromosome = new GradientChromosome(backgroundColor, numberOfGradients);
			for (int j = 0; j < numberOfGradients; j++)
			{
				chromosome.Genes.Add(new GradientGene
				{
					Position = new PointF((float)Rng.NextDouble(), (float)Rng.NextDouble()),
					Color = availableColors[Rng.Next(availableColors.Count)],
					Radius = Range(0.25f, 0.75f)
				});
			}
			population.Add(chromosome);
		}
		return population;
	}

	private static void CalculatePopulationFitness(
		List<GradientChromosome> population,
		Image<Rgba32> targetImage,
		float imageDiagonal)
	{
		foreach (var chromosome in population)
		{
			chromosome.Fitness = CalculateFitness(chromosome, targetImage, imageDiagonal);
		}
		population.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));
	}

	private static List<GradientChromosome> CreateNextGeneration(
		List<GradientChromosome> currentPopulation)
	{
		var newPopulation = new List<GradientChromosome>(PopulationSize);

		// Elitism
		for (int i = 0; i < ElitismCount; i++)
		{
			newPopulation.Add(currentPopulation[i].Clone());
		}

		// Crossover and mutation
		while (newPopulation.Count < PopulationSize)
		{
			var parent1 = SelectParent(currentPopulation);
			var parent2 = SelectParent(currentPopulation);
			var child = Crossover(parent1, parent2);
			Mutate(child);
			newPopulation.Add(child);
		}
		return newPopulation;
	}

	private static GradientChromosome SelectParent(
		List<GradientChromosome> population)
	{
		const int tournamentSize = 3;
		var tournamentContestants = new List<GradientChromosome>();
		for (int i = 0; i < tournamentSize; i++)
		{
			tournamentContestants.Add(population[Rng.Next(population.Count)]);
		}
		return tournamentContestants.OrderBy(c => c.Fitness).First();
	}

	private static GradientChromosome Crossover(
		GradientChromosome parent1,
		GradientChromosome parent2)
	{
		var child = new GradientChromosome(parent1.BackgroundColor, parent1.Genes.Count)
		{
			BackgroundColor = Rng.NextDouble() < 0.5 ? parent1.BackgroundColor : parent2.BackgroundColor
		};
		for (int i = 0; i < parent1.Genes.Count; i++)
		{
			child.Genes.Add(Rng.NextDouble() < 0.5 ? parent1.Genes[i].Clone() : parent2.Genes[i].Clone());
		}
		return child;
	}

	private static void Mutate(
		GradientChromosome chromosome)
	{
		// Mutate background color
		if (Rng.NextDouble() < MutationRate)
		{
			chromosome.BackgroundColor = new Rgb(
				Math.Clamp(chromosome.BackgroundColor.R + Range(-0.08f, 0.08f), 0.0f, 1.0f),
				Math.Clamp(chromosome.BackgroundColor.G + Range(-0.08f, 0.08f), 0.0f, 1.0f),
				Math.Clamp(chromosome.BackgroundColor.B + Range(-0.08f, 0.08f), 0.0f, 1.0f));
		}

		foreach (var gene in chromosome.Genes)
		{
			// Mutate position
			if (Rng.NextDouble() < MutationRate)
			{
				gene.Position = new PointF(
					Math.Clamp(gene.Position.X + Range(-0.1f, 0.1f), 0.0f, 1.0f),
					Math.Clamp(gene.Position.Y + Range(-0.1f, 0.1f), 0.0f, 1.0f));
			}

			// Mutate color
			if (Rng.NextDouble() < MutationRate)
			{
				gene.Color = new Rgb(
					Math.Clamp(gene.Color.R + Range(-0.08f, 0.08f), 0.0f, 1.0f),
					Math.Clamp(gene.Color.G + Range(-0.08f, 0.08f), 0.0f, 1.0f),
					Math.Clamp(gene.Color.B + Range(-0.08f, 0.08f), 0.0f, 1.0f));
			}

			// Mutate radius
			if (Rng.NextDouble() < MutationRate)
			{
				gene.Radius = Math.Clamp(gene.Radius + Range(-0.1f, 0.1f), 0.125f, 1.0f);
			}
		}
	}

	private static float CalculateFitness(
		GradientChromosome chromosome,
		Image<Rgba32> targetImage,
		float imageDiagonal)
	{
		var dimensions = targetImage.Size;

		float totalDifference = 0.0f;

		for (int y = 0; y < dimensions.Height; y++)
		{
			for (int x = 0; x < dimensions.Width; x++)
			{
				var generatedColor = GetGeneratedColorAtPixel(chromosome, dimensions, new Point(x, y), imageDiagonal);
				Rgb targetColor = targetImage[x, y];

				float diffR = generatedColor.R - targetColor.R;
				float diffG = generatedColor.G - targetColor.G;
				float diffB = generatedColor.B - targetColor.B;
				totalDifference += (diffR * diffR) + (diffG * diffG) + (diffB * diffB);
			}
		}
		return totalDifference;
	}

	private static Rgb GetGeneratedColorAtPixel(
		GradientChromosome chromosome,
		Size dimensions,
		Point position,
		float imageDiagonal)
	{
		var total = chromosome.BackgroundColor;

		foreach (var gene in chromosome.Genes)
		{
			var genePixelPosition = new Point(
				(int)(gene.Position.X * dimensions.Width),
				(int)(gene.Position.Y * dimensions.Height));
			float radiusPixels = gene.Radius * imageDiagonal * 0.5f;

			var difference = new Point(
				position.X - genePixelPosition.X,
				position.Y - genePixelPosition.Y);
			float distance = MathF.Sqrt((difference.X * difference.X) + (difference.Y * difference.Y));

			if (distance < radiusPixels)
			{
				float alpha = 1.0f - (distance / radiusPixels);
				total = Lerp(gene.Color, total, alpha);
			}
		}

		return total;
	}

	public static List<Rgb> GetDominantColors(
		Image<Rgba32> image,
		int numberOfColors,
		int maxIterations = 100,
		double tolerance = 0.1)
	{
		int totalPixels = image.Width * image.Height;
		if (totalPixels == 0)
		{
			return [];
		}

		var allPixels = new Rgb[totalPixels];
		int pixelIndex = 0;

		image.ProcessPixelRows(accessor =>
		{
			for (int y = 0; y < accessor.Height; y++)
			{
				var row = accessor.GetRowSpan(y);
				for (int x = 0; x < row.Length; x++)
				{
					allPixels[pixelIndex++] = row[x];
				}
			}
		});

		var centroids = new Rgb[numberOfColors];
		var rand = new Random();
		for (int i = 0; i < numberOfColors; i++)
		{
			centroids[i] = allPixels[rand.Next(allPixels.Length)];
		}

		int[] pixelClusterAssignments = new int[totalPixels];
		float[] clusterRSum = new float[numberOfColors];
		float[] clusterGSum = new float[numberOfColors];
		float[] clusterBSum = new float[numberOfColors];
		int[] clusterPixelCount = new int[numberOfColors];

		for (int iteration = 0; iteration < maxIterations; iteration++)
		{
			Array.Clear(clusterRSum, 0, numberOfColors);
			Array.Clear(clusterGSum, 0, numberOfColors);
			Array.Clear(clusterBSum, 0, numberOfColors);
			Array.Clear(clusterPixelCount, 0, numberOfColors);

			for (int i = 0; i < totalPixels; i++)
			{
				var currentPixel = allPixels[i];
				ref int closestCentroidIndex = ref pixelClusterAssignments[i];
				float minDistance = float.MaxValue;

				for (int j = 0; j < numberOfColors; j++)
				{
					float distance = GetColorDistance(currentPixel, centroids[j]);
					if (distance < minDistance)
					{
						minDistance = distance;
						closestCentroidIndex = j;
					}
				}

				clusterRSum[closestCentroidIndex] += currentPixel.R;
				clusterGSum[closestCentroidIndex] += currentPixel.G;
				clusterBSum[closestCentroidIndex] += currentPixel.B;
				clusterPixelCount[closestCentroidIndex]++;
			}

			var newCentroids = new Rgb[numberOfColors];
			double totalCentroidMovement = 0;

			for (int i = 0; i < numberOfColors; i++)
			{
				int c = clusterPixelCount[i];

				if (c > 0)
				{
					float r = clusterRSum[i] / c;
					float g = clusterGSum[i] / c;
					float b = clusterBSum[i] / c;
					var newCentroid = new Rgb(r, g, b);

					totalCentroidMovement += GetColorDistance(centroids[i], newCentroid);
					newCentroids[i] = newCentroid;
				}
				else
				{
					newCentroids[i] = allPixels[rand.Next(allPixels.Length)];
				}
			}

			Array.Copy(newCentroids, centroids, numberOfColors);

			if (totalCentroidMovement < tolerance)
			{
				break;
			}
		}

		var colorCounts = new Dictionary<Rgb, int>();
		for (int i = 0; i < totalPixels; i++)
		{
			var dominantColor = centroids[pixelClusterAssignments[i]];

			if (colorCounts.TryGetValue(dominantColor, out int currentCount))
			{
				colorCounts[dominantColor] = currentCount + 1;
			}
			else
			{
				colorCounts[dominantColor] = 1;
			}
		}

		var dominantColors = colorCounts
			.OrderByDescending(kvp => kvp.Value)
			.Select(kvp => kvp.Key)
			.ToList();

		return dominantColors;
	}

	public static Rgb Lerp(
		Rgb start,
		Rgb end,
		float alpha)
	{
		return new Rgb(
			(start.R * alpha) + (end.R * (1.0f - alpha)),
			(start.G * alpha) + (end.G * (1.0f - alpha)),
			(start.B * alpha) + (end.B * (1.0f - alpha)));
	}

	public static float Range(
		float minimum,
		float maximum)
	{
		return ((float)Rng.NextDouble() * (maximum - minimum)) + minimum;
	}

	private static float GetColorDistance(
		Rgb color1,
		Rgb color2)
	{
		float dr = color1.R - color2.R;
		float dg = color1.G - color2.G;
		float db = color1.B - color2.B;
		return MathF.Sqrt((dr * dr) + (dg * dg) + (db * db));
	}

	private static string ConvertToCss(
		GradientChromosome chromosome)
	{
		var gradients = new List<string>();
		foreach (var gene in chromosome.Genes)
		{
			float positionX = gene.Position.X * 100;
			float positionY = gene.Position.Y * 100;

			gradients.Add($"radial-gradient(at {positionX:0.#}% {positionY:0.#}%, {Rgba32ToHexString(gene.Color)} 0, {Rgba32ToTransparentHexString(gene.Color)} {gene.Radius * 100:0.#}%)");
		}

		string cssBackgroundColor = $"background-color: {Rgba32ToHexString(chromosome.BackgroundColor)};";
		if (gradients.Count == 0)
		{
			return cssBackgroundColor;
		}

		string cssGradients = $"background-image: {string.Join(", ", gradients)};";
		return $"{cssBackgroundColor}\n{cssGradients}";
	}

	public static string Rgba32ToHexString(Rgba32 color)
	{
		if (color.A == 255)
		{
			return $"#{color.R:x2}{color.G:x2}{color.B:x2}";
		}

		return $"#{color.R:x2}{color.G:x2}{color.B:x2}{color.A:x2}";
	}

	public static string Rgba32ToTransparentHexString(Rgba32 color)
	{
		return $"#{color.R:x2}{color.G:x2}{color.B:x2}00";
	}
}
