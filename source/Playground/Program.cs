// See https://aka.ms/new-console-template for more information

using DacPac.Core;
using Microsoft.Extensions.Logging.Abstractions;

DockerService service = new DockerService(NullLogger<DockerService>.Instance);
var listContainers = await service.ListContainers().ToListAsync(); 