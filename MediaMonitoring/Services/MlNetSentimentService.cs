using MediaMonitoring.Data;
using MediaMonitoring.Models.ML;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;

namespace MediaMonitoring.Services
{
    public class MlNetSentimentService : IDisposable
    {
        private readonly MLContext _mlContext;
        private ITransformer? _trainedModel;
        private PredictionEngine<SentimentData, SentimentPrediction>? _predictionEngine;
        private readonly string _modelPath;

        public MlNetSentimentService()
        {
            _mlContext = new MLContext(seed: 0);
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MLModels", "sentiment_model.zip");
            InitializeModel();
        }

        private void InitializeModel()
        {
            if (File.Exists(_modelPath))
            {
                try
                {
                    _trainedModel = _mlContext.Model.Load(_modelPath, out var schema);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_trainedModel);
                    Console.WriteLine("ML.NET Sentiment Model loaded");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading ML model: {ex.Message}. Training new...");
                    TrainModel();
                }
            }
            else
            {
                TrainModel();
            }
        }

        private void TrainModel()
        {
            Console.WriteLine("Training ML.NET sentiment model...");

            var trainingData = new List<SentimentData>
            {
                new SentimentData { Text = "Saya sangat senang dengan hasil ini" },
                new SentimentData { Text = "Produk yang bagus sekali" },
                new SentimentData { Text = "Keren banget, mantap" },
                new SentimentData { Text = "Hebat, sukses terus!" },
                new SentimentData { Text = "Saya kecewa dengan produk ini" },
                new SentimentData { Text = "Jelek banget" },
                new SentimentData { Text = "Gagal total" },
                new SentimentData { Text = "Konflik makin panas" }
            };

            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());

            _trainedModel = pipeline.Fit(dataView);
            
            Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
            _mlContext.Model.Save(_trainedModel, dataView.Schema, _modelPath);
            
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(_trainedModel);
            Console.WriteLine("ML Model trained and saved");
        }

        public (string Label, double Score, bool IsConfident) AnalyzeSentiment(string text)
        {
            if (_predictionEngine == null) return FallbackToRuleBased(text);

            try
            {
                var prediction = _predictionEngine.Predict(new SentimentData { Text = text });
                string label = prediction.PredictedSentiment ? "Positive" : "Negative";
                double score = prediction.PredictedSentiment ? prediction.Probability : -prediction.Probability;
                
                if (Math.Abs(prediction.Probability - 0.5f) <= 0.1f)
                {
                    label = "Neutral";
                    score = 0.0;
                }

                return (label, Math.Round(score, 2), Math.Abs(prediction.Probability - 0.5f) > 0.2f);
            }
            catch
            {
                return FallbackToRuleBased(text);
            }
        }

        private (string Label, double Score, bool IsConfident) FallbackToRuleBased(string text)
        {
            var lowerText = text.ToLower();
            double score = 0.0;

            var positiveWords = new[] { "senang", "bagus", "hebat", "sukses", "menang", "keren", "mantap" };
            var negativeWords = new[] { "kecewa", "jelek", "gagal", "kalah", "rugi", "konflik" };

            foreach (var word in positiveWords) if (lowerText.Contains(word)) score += 0.2;
            foreach (var word in negativeWords) if (lowerText.Contains(word)) score -= 0.2;

            score = Math.Max(-1, Math.Min(1, score));
            string label = score > 0.15 ? "Positive" : score < -0.15 ? "Negative" : "Neutral";

            return (label, Math.Round(score, 2), Math.Abs(score) > 0.3);
        }

        public async Task RetrainWithDataAsync(MediaMonitoringContext context)
        {
            var posts = await context.MediaPosts.Where(p => p.IsProcessed).Take(1000).ToListAsync();
            if (posts.Count < 10) return;
            
            Console.WriteLine("Retraining ML model...");
            TrainModel();
        }

        public void Dispose()
        {
            _predictionEngine?.Dispose();
        }
    }
}