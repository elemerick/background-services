using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TennisBookings.Processing;
using TennisBookings.ResultsProcessing;

namespace TennisBookings.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/[controller]")]
[Authorize(Roles = "Admin")]
public class ResultsController : Controller
{
	private readonly IResultProcessor _resultProcessor;
	private readonly ILogger<ResultsController> _logger;
	private readonly FileProcessingChannel _fileProcessingChannel;

	public ResultsController(
		IResultProcessor resultProcessor,
		ILogger<ResultsController> logger,
		FileProcessingChannel fileProcessingChannel)
	{
		_resultProcessor = resultProcessor;
		_logger = logger;
		_fileProcessingChannel = fileProcessingChannel;
	}

	[HttpGet]
	public IActionResult UploadResults()
	{
		return View();
	}

	[HttpGet("v2")]
	public IActionResult UploadResultsV2()
	{
		return View();
	}

	[HttpGet("v3")]
	public IActionResult UploadResultsV3()
	{
		return View();
	}

	[HttpPost("FileUpload")]
	public async Task<IActionResult> FileUpload(IFormFile file, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		if (file is object && file.Length > 0)
		{
			var fileName = Path.GetTempFileName(); // Upload to a temp file path

			await using var stream = new FileStream(fileName, FileMode.Create);

			await file.CopyToAsync(stream, cancellationToken);

			stream.Position = 0;

			await _resultProcessor.ProcessAsync(stream, cancellationToken);

			System.IO.File.Delete(fileName); // Delete the temp file
		}

		sw.Stop();

		_logger.LogInformation("Time taken for result upload and processing " +
			"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

		return RedirectToAction("UploadComplete");
	}

	[HttpPost("FileUploadV2")]
	public async Task<IActionResult> FileUploadV2(IFormFile file, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		if (file is object && file.Length > 0)
		{
			// TODO
			var fileName = Path.GetTempFileName();
			using (var stream = new FileStream($"{fileName}", FileMode.Create, FileAccess.Write))
			{
				await stream.CopyToAsync(stream, cancellationToken);
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(TimeSpan.FromSeconds(3));

			try
			{
				var fileWritten = _fileProcessingChannel.AddFileAsync(fileName, cts.Token);
			}
			catch (OperationCanceledException) when (cts.IsCancellationRequested) {
				System.IO.File.Delete(fileName);
			}
		}

		sw.Stop();

		_logger.LogInformation("Time taken for result upload and processing " +
			"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

		return RedirectToAction("UploadFailed");
	}

	[HttpPost("FileUploadV3")]
	public async Task<IActionResult> FileUploadV3(IFormFile file, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		if (file is object && file.Length > 0)
		{
			// TODO
		}

		sw.Stop();

		_logger.LogInformation("Time taken for result upload and processing " +
			"was {ElapsedMilliseconds}ms.", sw.ElapsedMilliseconds);

		return RedirectToAction("UploadComplete");
	}

	[HttpGet("FileUploadComplete")]
	public IActionResult UploadComplete()
	{
		return View();
	}

	[HttpGet("FileUploadFailed")]
	public IActionResult UploadFailed()
	{
		return View();
	}
}
