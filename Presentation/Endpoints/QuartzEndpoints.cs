using Microsoft.AspNetCore.Mvc;
using Quartz;
using Quartz.Impl.Matchers;
using Application.Response.JobResp;

namespace Presentation.Endpoints
{
    public static class QuartzEndpoints
    {
        public static void MapQuartzEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/admin/quartz")
                           .WithTags("Quartz Management");
            //.RequireAuthorization();

            // 1. Lấy danh sách Jobs
            group.MapGet("/jobs", async ([FromServices] ISchedulerFactory schedulerFactory) =>
            {
                // Sử dụng factory để lấy scheduler, đảm bảo service đã sẵn sàng
                var scheduler = await schedulerFactory.GetScheduler();
                var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
                var jobs = new List<JobResponse>();

                foreach (var jobKey in jobKeys)
                {
                    var detail = await scheduler.GetJobDetail(jobKey);
                    if (detail != null)
                    {
                        jobs.Add(new JobResponse(
                            jobKey.Name,
                            jobKey.Group,
                            detail.Description,
                            detail.Durable));
                    }
                }
                return Results.Ok(jobs);
            });

            // 2. Lấy danh sách Triggers
            group.MapGet("/triggers", async ([FromServices] ISchedulerFactory schedulerFactory) =>
            {
                var scheduler = await schedulerFactory.GetScheduler();
                var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
                var triggers = new List<TriggerResponse>();

                foreach (var key in triggerKeys)
                {
                    var t = await scheduler.GetTrigger(key);
                    var state = await scheduler.GetTriggerState(key);

                    if (t != null)
                    {
                        triggers.Add(new TriggerResponse(
                            key.Name,
                            key.Group,
                            t.JobKey.Name,
                            t.JobKey.Group,
                            t.GetNextFireTimeUtc()?.LocalDateTime,
                            t.GetPreviousFireTimeUtc()?.LocalDateTime,
                            state.ToString(),
                            t.GetType().Name.Replace("Impl", "")));
                    }
                }
                return Results.Ok(triggers);
            });

            // 3. Thực thi ngay lập tức (Run Now)
            group.MapPost("/triggers/{group}/{name}/run", async (
                [FromServices] ISchedulerFactory schedulerFactory,
                [FromRoute] string group,
                [FromRoute] string name) =>
            {
                var scheduler = await schedulerFactory.GetScheduler();
                var triggerKey = new TriggerKey(name, group);
                var trigger = await scheduler.GetTrigger(triggerKey);

                if (trigger == null)
                    return Results.NotFound(new { Message = "Không tìm thấy Trigger" });

                await scheduler.TriggerJob(trigger.JobKey);
                return Results.Ok(new { Message = $"Đã kích hoạt Job: {trigger.JobKey.Name}" });
            });

            // 4. Tạm dừng (Pause)
            group.MapPost("/triggers/{group}/{name}/pause", async (
                [FromServices] ISchedulerFactory schedulerFactory,
                [FromRoute] string group,
                [FromRoute] string name) =>
            {
                var scheduler = await schedulerFactory.GetScheduler();
                await scheduler.PauseTrigger(new TriggerKey(name, group));
                return Results.Ok(new { Message = "Đã tạm dừng" });
            });

            // 5. Tiếp tục (Resume)
            group.MapPost("/triggers/{group}/{name}/resume", async (
                [FromServices] ISchedulerFactory schedulerFactory,
                [FromRoute] string group,
                [FromRoute] string name) =>
            {
                var scheduler = await schedulerFactory.GetScheduler();
                await scheduler.ResumeTrigger(new TriggerKey(name, group));
                return Results.Ok(new { Message = "Đã kích hoạt lại" });
            });

            // 6. Xóa Trigger
            group.MapDelete("/triggers/{group}/{name}", async (
                [FromServices] ISchedulerFactory schedulerFactory,
                [FromRoute] string group,
                [FromRoute] string name) =>
            {
                var scheduler = await schedulerFactory.GetScheduler();
                var result = await scheduler.UnscheduleJob(new TriggerKey(name, group));
                return result ? Results.Ok(new { Message = "Đã xóa lịch trình" }) : Results.BadRequest();
            });
        }
    }
}