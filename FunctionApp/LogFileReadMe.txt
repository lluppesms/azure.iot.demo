****************************************************************************************************
View log entries in Application Insights using a query like this:
****************************************************************************************************
traces 
| project eventDateTime, message, severityLevel, customDimensions
//| where customDimensions.eventSource == "API" or customDimensions.eventSource == "SvcBus"
//| filter eventDateTime >= datetime(2022-07-02 21:07:00Z)  
| filter timestamp >= ago(15m) // ago(1d) ago(8h) ago(15m)
| extend source = tostring(customDimensions["eventSource"])
| extend serialNumber = tostring(customDimensions["serialNumber"])
| take 100
| order by eventDateTime desc 

****************************************************************************************************
View every time this function was called, not just the trace info
****************************************************************************************************
requests
| project
    timestamp,id,operation_Name,success,resultCode,duration,operation_Id,cloud_RoleName,invocationId=customDimensions['InvocationId']
| where timestamp > ago(1d)
| where cloud_RoleName =~ 'lciiotprocessdev1'
// and operation_Name =~ 'TriggerFileUpload'
| order by timestamp desc
| take 200
