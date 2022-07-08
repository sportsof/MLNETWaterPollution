using ConsoleAppWaterPollution.Models;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppWaterPollution
{
    public class Forecaster
    {
        MLContext _mlContext;

        public Forecaster()
        {
            _mlContext = new MLContext();
        }

        public IDataView LoadFromEnumerable(IEnumerable<Pollution> trainingData)
        {
            return _mlContext.Data.LoadFromEnumerable(trainingData);
        }

        public SsaForecastingTransformer GetSsaForecastingTransformer(SsaForecastingEstimator estimator, IDataView dataView)
        {
            return estimator.Fit(dataView);
        }

        public TimeSeriesPredictionEngine<Pollution, ModelOutput> GetForecastEngine(SsaForecastingTransformer transformer)
        {
            var forecastEngine = transformer.CreateTimeSeriesEngine<Pollution, ModelOutput>(_mlContext);

            return forecastEngine;
        }

        public ModelOutput Forecast(int horizon, TimeSeriesPredictionEngine<Pollution, ModelOutput> forecastEngine)
        {
            return forecastEngine.Predict(horizon);
        }

        public SsaForecastingEstimator TrainWithForecast(string propertyName, int windowSize, int trainSize)
        {
            return _mlContext.Forecasting.ForecastBySsa(
                                outputColumnName: "Values",
                                inputColumnName: propertyName,
                                windowSize: windowSize,
                                seriesLength: windowSize * 2,
                                trainSize: trainSize,
                                horizon: 3,
                                confidenceLevel: 0.95f,
                                confidenceLowerBoundColumn: "LowerBoundValues",
                                confidenceUpperBoundColumn: "UpperBoundValues");
        }

        public double GetMetric(IDataView dataView, float[] predictions)
        {
            IEnumerable<float> actual = _mlContext.Data.CreateEnumerable<Pollution>(dataView, true)
                .AsQueryable()
                .Select<float>("Cnt_cases");

            var forecast = predictions.Select(x =>
            {
                //if (x < 0) return 0;

                return x;
            }).ToList();

            var metrics = actual.Zip(forecast, (actualValue, forecastValue) => actualValue - forecastValue);

            // Mean Absolute Error
            var MAE = metrics.Average(error => Math.Abs(error));

            // Root Mean Squared Error
            var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2))); 

            return RMSE;
        }
    }
}
