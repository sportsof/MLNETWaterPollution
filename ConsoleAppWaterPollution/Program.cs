using ConsoleAppWaterPollution;
using ConsoleAppWaterPollution.Models;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Globalization;
using System.Text;

// Вологодская область	р. Волга	Нитрит-ионы

var config = new CsvConfiguration(CultureInfo.CurrentCulture) { Delimiter = ",", Encoding = Encoding.UTF8 };

using var streamReader = File.OpenText("high_pollution.csv");
using var csvReader = new CsvReader(streamReader, config);

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
int prognosisPeriod = 3;

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

using var writer = new StreamWriter("submission.csv");
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

    var trainData = completeDataWithGroup.Take(completeDataWithGroup.Count - prognosisPeriod).ToList();
    var testData = completeDataWithGroup.Skip(completeDataWithGroup.Count - prognosisPeriod).Take(prognosisPeriod).ToList();

    IDataView trainingViewData = forecaster.LoadFromEnumerable(trainData);
    IDataView testViewData = forecaster.LoadFromEnumerable(testData);

    // Создание модели
    var estimator = forecaster.TrainWithForecast("Cnt_cases", windowSize, (int)trainingViewData.GetRowCount());

    // Обучение
    var transformer = forecaster.GetSsaForecastingTransformer(estimator, trainingViewData);

    // Создание движка для прогнозирования.
    // На этом этапе можно зафиксировать модель в файл
    var forecastEngine = forecaster.GetForecastEngine(transformer);

    var modelOutput = forecaster.Forecast(prognosisPeriod, forecastEngine);

    float v = modelOutput.Values[0] < 0 ? 0f : modelOutput.Values[0];

    csvWriter.WriteField(group.Subject);
    csvWriter.WriteField(group.River_basin);
    csvWriter.WriteField(group.Indicator);
    csvWriter.WriteField(testData[0].Cnt_cases);
    csvWriter.WriteField(v);
    csvWriter.NextRecord();

    rmse += forecaster.GetMetric(testViewData, modelOutput.Values);
}

Console.WriteLine($"RMSE: {rmse}");