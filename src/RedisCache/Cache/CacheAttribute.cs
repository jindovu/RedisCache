﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RedisCache.Cache
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CacheAttribute : ResultFilterAttribute, IActionFilter
    {
        protected ICacheService CacheService { set; get; }

        public CacheAttribute()
        {
            
        }

        public int Duration { set; get; }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            GetServices(context);
            var requestUrl = context.HttpContext.Request.GetEncodedUrl();
            var cacheKey = requestUrl.ToMd5();
            var cachedResult = CacheService.Get<string>(cacheKey);
            var contentType = CacheService.Get<string>(cacheKey + "_contentType");
            var statusCode = CacheService.Get<string>(cacheKey + "_statusCode");
            if (!string.IsNullOrEmpty(cachedResult) && !string.IsNullOrEmpty(contentType) &&
                !string.IsNullOrEmpty(statusCode))
            {
                //cache hit
                var httpResponse = context.HttpContext.Response;
                httpResponse.ContentType = contentType;
                httpResponse.StatusCode = Convert.ToInt32(statusCode);

                var responseStream = httpResponse.Body;
                responseStream.Seek(0, SeekOrigin.Begin);
                if (responseStream.Length <= cachedResult.Length)
                {
                    responseStream.SetLength((long)cachedResult.Length << 1);
                }
                using (var writer = new StreamWriter(responseStream, Encoding.UTF8, 4096, true))
                {
                    writer.Write(cachedResult);
                    writer.Flush();
                    responseStream.Flush();
                    context.Result = new ContentResult { Content = cachedResult };
                }
            }
            else
            {
                //cache miss
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            //nothing for you there
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is ContentResult)
            {
                context.Cancel = true;
            }
        }



        public override void OnResultExecuted(ResultExecutedContext context)
        {
            GetServices(context);
            var cacheKey = context.HttpContext.Request.GetEncodedUrl().ToMd5();
            var httpResponse = context.HttpContext.Response;
            var responseStream = httpResponse.Body;
            responseStream.Seek(0, SeekOrigin.Begin);
            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8, true, 512, true))
            {
                var toCache = streamReader.ReadToEnd();
                var contentType = httpResponse.ContentType;
                var statusCode = httpResponse.StatusCode.ToString();
                Task.Factory.StartNew(() =>
                {
                    CacheService.Store(cacheKey + "_contentType", contentType, Duration);
                    CacheService.Store(cacheKey + "_statusCode", statusCode, Duration);
                    CacheService.Store(cacheKey, toCache, Duration);
                });

            }
            base.OnResultExecuted(context);
        }
        protected void GetServices(FilterContext context)
        {
            CacheService = context.HttpContext.RequestServices.GetService(typeof(ICacheService)) as ICacheService;
        }
    }
}
