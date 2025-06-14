﻿using DeliCheck.Api.Models;

namespace DeliCheck.Services
{
    /// <summary>
    /// Интерфейс для сервиса распознования текста
    /// </summary>
    public interface IOcrService
    {
        /// <summary>
        /// Получить текст из изображения
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <returns>Текст с изображения</returns>
        Task<OcrResult> GetTextFromImageAsync(string imagePath);
    }
}
