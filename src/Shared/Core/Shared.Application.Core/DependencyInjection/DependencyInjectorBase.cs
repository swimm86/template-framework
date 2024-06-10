// ----------------------------------------------------------------------------------------------
// <copyright file="DependencyInjectorBase.cs" company="ООО Газпромнефть - Цифровые решения">
// Copyright (c) ООО Газпромнефть - Цифровые решения. All rights reserved.
// </copyright>
// ----------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Application.Core.DependencyInjection
{
    /// /// <summary>
    /// Абстрактный базовый класс для реализации внедрения зависимостей.
    /// Предоставляет шаблонный метод для внедрения зависимостей в коллекцию сервисов.
    /// </summary>
    /// /// <param name="logger">Логгер</param>
    public abstract class DependencyInjectorBase(ILogger logger)
    {
        /// <summary>
        /// Обертка для внедрения зависимостей
        /// </summary>
        /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
        /// <returns>Коллекция сервисов <see cref="IServiceCollection"/>.</returns>
        public IServiceCollection Inject(IServiceCollection serviceCollection)
        {
            try
            {
                var result = Process(serviceCollection);
                logger.LogInformation("Инфраструктура внедрена");
                return result;
            }
            catch
            {
                logger.LogError("Не удалось внедрить инфраструктуру");
                throw;
            }
        }

        /// <summary>
        /// Процесс внедрения зависимостей
        /// </summary>
        /// <param name="serviceCollection">Коллекция сервисов <see cref="IServiceCollection"/>.</param>
        /// <returns>Коллекция сервисов <see cref="IServiceCollection"/>.</returns>
        protected abstract IServiceCollection Process(IServiceCollection serviceCollection);
    }
}
