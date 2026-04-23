// ----------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="swimm86@yandex.ru">
// Copyright (c) swimm86@yandex.ru. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Shared.Presentation.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddSharedPresentationCore();

var app = builder.Build();
app.ConfigurePresentationCore();

app.Run();
