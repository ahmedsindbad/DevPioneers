@echo off
REM ============================================
REM إصلاح خطأ Generic Constraint في CachingBehavior.cs
REM ============================================
chcp 65001 >nul

echo.
echo 🔧 إصلاح CachingBehavior.cs - Generic Constraint Error
echo ================================================

REM Set base path - غير هذا المسار حسب مجلدك
set "BASE_PATH=G:\Projects\DevPioneers\src\DevPioneers.Application"

echo 📁 المسار: %BASE_PATH%\Common\Behaviors\
echo.

REM Create backup of original file
if exist "%BASE_PATH%\Common\Behaviors\CachingBehavior.cs" (
    echo 📋 إنشاء نسخة احتياطية...
    copy "%BASE_PATH%\Common\Behaviors\CachingBehavior.cs" "%BASE_PATH%\Common\Behaviors\CachingBehavior.cs.bak" >nul
    echo ✅ تم إنشاء نسخة احتياطية: CachingBehavior.cs.bak
)

echo.
echo 🔧 إنشاء الملف المُصحح...

REM Create the fixed CachingBehavior.cs
(
echo // ============================================
echo // File: DevPioneers.Application/Common/Behaviors/CachingBehavior.cs ^(Fixed^)
echo // ============================================
echo using DevPioneers.Application.Common.Interfaces;
echo using MediatR;
echo using Microsoft.Extensions.Logging;
echo using System.Text.Json;
echo.
echo namespace DevPioneers.Application.Common.Behaviors;
echo.
echo /// ^<summary^>
echo /// MediatR pipeline behavior for caching query responses
echo /// Only works with IRequest^<TResponse^> where TResponse is a class ^(reference type^)
echo /// ^</summary^>
echo public class CachingBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : IRequest^<TResponse^>
echo     where TResponse : class // Add class constraint to ensure TResponse is a reference type
echo {
echo     private readonly ICacheService _cacheService;
echo     private readonly ILogger^<CachingBehavior^<TRequest, TResponse^>^> _logger;
echo.
echo     public CachingBehavior^(
echo         ICacheService cacheService,
echo         ILogger^<CachingBehavior^<TRequest, TResponse^>^> logger^)
echo     {
echo         _cacheService = cacheService;
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request, 
echo         RequestHandlerDelegate^<TResponse^> next, 
echo         CancellationToken cancellationToken^)
echo     {
echo         // Only cache if request implements ICacheableQuery
echo         if ^(request is not ICacheableQuery cacheableQuery^)
echo         {
echo             // Not cacheable, proceed normally
echo             return await next^(^);
echo         }
echo.
echo         // Generate cache key
echo         var cacheKey = GenerateCacheKey^(request, cacheableQuery^);
echo.
echo         // Try to get from cache
echo         try
echo         {
echo             var cachedResponse = await _cacheService.GetAsync^<TResponse^>^(cacheKey, cancellationToken^);
echo             if ^(cachedResponse != null^)
echo             {
echo                 _logger.LogInformation^("Cache hit for key: {CacheKey}", cacheKey^);
echo                 return cachedResponse;
echo             }
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogWarning^(ex, "Failed to retrieve from cache for key: {CacheKey}", cacheKey^);
echo         }
echo.
echo         // Cache miss - execute request
echo         _logger.LogDebug^("Cache miss for key: {CacheKey}", cacheKey^);
echo         var response = await next^(^);
echo.
echo         // Cache the response if not null
echo         if ^(response != null^)
echo         {
echo             try
echo             {
echo                 var expiration = cacheableQuery.GetCacheExpiration^(^);
echo                 await _cacheService.SetAsync^(cacheKey, response, expiration, cancellationToken^);
echo                 _logger.LogDebug^("Cached response for key: {CacheKey} with expiration: {Expiration}", 
echo                     cacheKey, expiration^);
echo             }
echo             catch ^(Exception ex^)
echo             {
echo                 _logger.LogWarning^(ex, "Failed to cache response for key: {CacheKey}", cacheKey^);
echo             }
echo         }
echo.
echo         return response;
echo     }
echo.
echo     /// ^<summary^>
echo     /// Generate cache key based on request type and properties
echo     /// ^</summary^>
echo     private string GenerateCacheKey^(TRequest request, ICacheableQuery cacheableQuery^)
echo     {
echo         var requestTypeName = typeof^(TRequest^).Name;
echo         var customKey = cacheableQuery.GetCacheKey^(^);
echo         
echo         if ^(!string.IsNullOrEmpty^(customKey^)^)
echo         {
echo             return $"{requestTypeName}:{customKey}";
echo         }
echo.
echo         // Generate key from request properties
echo         try
echo         {
echo             var requestJson = JsonSerializer.Serialize^(request, new JsonSerializerOptions
echo             {
echo                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
echo                 WriteIndented = false
echo             }^);
echo.
echo             // Create a hash of the JSON for consistent key generation
echo             var hash = GetStringHash^(requestJson^);
echo             return $"{requestTypeName}:{hash}";
echo         }
echo         catch ^(Exception ex^)
echo         {
echo             _logger.LogWarning^(ex, "Failed to generate cache key from request properties for {RequestType}", requestTypeName^);
echo             // Fallback to simple key
echo             return $"{requestTypeName}:fallback";
echo         }
echo     }
echo.
echo     /// ^<summary^>
echo     /// Generate consistent hash from string
echo     /// ^</summary^>
echo     private string GetStringHash^(string input^)
echo     {
echo         var hash = 0;
echo         foreach ^(char c in input^)
echo         {
echo             hash = ^(^(hash ^<^< 5^) - hash^) + c;
echo             hash = hash ^& hash; // Convert to 32-bit integer
echo         }
echo         return Math.Abs^(hash^).ToString^(^);
echo     }
echo }
echo.
echo /// ^<summary^>
echo /// Interface to mark queries as cacheable
echo /// ^</summary^>
echo public interface ICacheableQuery
echo {
echo     /// ^<summary^>
echo     /// Get cache key for the query ^(optional - can return null for auto-generation^)
echo     /// ^</summary^>
echo     string? GetCacheKey^(^) =^> null;
echo.
echo     /// ^<summary^>
echo     /// Get cache expiration time
echo     /// ^</summary^>
echo     TimeSpan? GetCacheExpiration^(^) =^> TimeSpan.FromMinutes^(5^); // Default 5 minutes
echo }
echo.
echo /// ^<summary^>
echo /// Alternative caching behavior for value types ^(if needed^)
echo /// This version doesn't use caching but logs for debugging
echo /// ^</summary^>
echo public class ValueTypeCachingBehavior^<TRequest, TResponse^> : IPipelineBehavior^<TRequest, TResponse^>
echo     where TRequest : IRequest^<TResponse^>
echo     where TResponse : struct // For value types
echo {
echo     private readonly ILogger^<ValueTypeCachingBehavior^<TRequest, TResponse^>^> _logger;
echo.
echo     public ValueTypeCachingBehavior^(ILogger^<ValueTypeCachingBehavior^<TRequest, TResponse^>^> logger^)
echo     {
echo         _logger = logger;
echo     }
echo.
echo     public async Task^<TResponse^> Handle^(
echo         TRequest request, 
echo         RequestHandlerDelegate^<TResponse^> next, 
echo         CancellationToken cancellationToken^)
echo     {
echo         // Value types are not cached in this implementation
echo         // You could implement specialized caching here if needed
echo         _logger.LogDebug^("Processing value type request: {RequestType}", typeof^(TRequest^).Name^);
echo         return await next^(^);
echo     }
echo }
) > "%BASE_PATH%\Common\Behaviors\CachingBehavior.cs"

echo ✅ تم إنشاء الملف المُصحح بنجاح!
echo.
echo 📋 التغييرات المُطبقة:
echo =============================
echo    ✅ إضافة constraint: where TResponse : class
echo    ✅ إضافة interface ICacheableQuery
echo    ✅ إضافة ValueTypeCachingBehavior للأنواع البدائية
echo    ✅ تحسين معالجة الأخطاء
echo    ✅ تحسين توليد cache keys
echo.
echo 💡 ملاحظة مهمة:
echo ===============
echo    - CachingBehavior يعمل الآن فقط مع reference types
echo    - استخدم ICacheableQuery في queries التي تريد تخزينها مؤقتاً
echo    - ValueTypeCachingBehavior متاح للأنواع البدائية ^(بدون تخزين مؤقت^)
echo.
echo 🚀 الخطوة التالية:
echo =================
echo    1. اختبر البناء: dotnet build
echo    2. تأكد من عدم وجود أخطاء compilation
echo    3. يمكنك حذف الملف الاحتياطي إذا كان كل شيء يعمل
echo.
echo 🎉 تم إصلاح CachingBehavior بنجاح!
echo.

pause