using ConsoleAppWaterPollution;
using ConsoleAppWaterPollution.Models;
using CsvHelper;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Globalization;

// Вологодская область	р. Волга	Нитрит-ионы

using var streamReader = File.OpenText("high_pollution.csv");
using var csvReader = new CsvReader(streamReader, CultureInfo.CurrentCulture);

var pollutuinsRaw = csvReader.GetRecords<PollutionRaw>();
var trainingData = new List<Pollution>();

foreach (var poll in pollutuinsRaw)
{
    var period = Convert.ToDateTime(poll.period);
    var Value_min = Convert.ToSingle(poll.value_min.Replace('.', ','));
    var Value_max = Convert.ToSingle(poll.value_max.Replace('.', ','));

    var p = new Pollution
    {
        Id = poll.id,
        Period = period,
        Okato = poll.okato,
        Subject = poll.subject,
        River_basin = poll.river_basin,
        Indicator = poll.indicator,
        Hazard_class = poll.hazard_class,
        Cnt_cases = poll.cnt_cases,
        Value_min = Value_min,
        Value_max = Value_max,
        Unit = poll.unit
    };
    trainingData.Add(p);
}

int windowSize = 12;

var forecaster = new Forecaster();

// Группировка данных по: область, название реки и название загрязняющего вещества
var trainingDataGroups = trainingData
    .GroupBy(g => new { g.Subject, g.River_basin, g.Indicator })
    .Select(x => new PollutionGroup { 
        Subject = x.Key.Subject, 
        River_basin = x.Key.River_basin, 
        Indicator = x.Key.Indicator })
    .ToList();

double rmse = 0;

using var writer = new StreamWriter("x.csv");
using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

csvWriter.WriteField("Subject");
csvWriter.WriteField("River");
csvWriter.WriteField("Indicator");
csvWriter.WriteField("Actual");
csvWriter.WriteField("Forecast");
csvWriter.NextRecord();

foreach (var group in trainingDataGroups)
{
    var dataWithGroup = trainingData
        .Where(x => x.Subject == group.Subject && x.River_basin == group.River_basin && x.Indicator == group.Indicator)
        .OrderBy(x => x.Period)
        .ToList();
    var completeDataWithGroup = DataManipulator.GetCompletePollution(dataWithGroup);

    var trainData = completeDataWithGroup.Take(completeDataWithGroup.Count - 3).ToList();
    var testData = completeDataWithGroup.Skip(completeDataWithGroup.Count - 3).Take(3).ToList();

    IDataView trainingViewData = forecaster.LoadFromEnumerable(trainData);
    IDataView testViewData = forecaster.LoadFromEnumerable(testData);

    var estimator = forecaster.TrainWithForecast("Cnt_cases", windowSize, (int)trainingViewData.GetRowCount());

    var transformer = forecaster.GetSsaForecastingTransformer(estimator, trainingViewData);

    var forecastEngine = forecaster.GetForecastEngine(transformer);

    var modelOutput = forecaster.Forecast(3, forecastEngine);

    csvWriter.WriteField(group.Subject);
    csvWriter.WriteField(group.River_basin);
    csvWriter.WriteField(group.Indicator);
    csvWriter.WriteField(testData[0].Cnt_cases);
    csvWriter.WriteField(modelOutput.Values[0]);
    csvWriter.NextRecord();

    rmse += forecaster.GetMetric(testViewData, modelOutput.Values);
}

Console.WriteLine($"RMSE: {rmse}");