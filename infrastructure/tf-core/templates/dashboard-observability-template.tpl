{
    "version": "1.0.0",
    "metadata": {
        "model": "portal.observability.dashboard",
        "title": "Portal Observability Dashboard",
        "subtitle": "Observability overview"
    },
    "lenses": {
        "0": {
            "order": 0,
            "parts": {
                "0": {
                    "position": {
                        "x": 0,
                        "y": 0,
                        "colSpan": 2,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "id",
                                "value": "/subscriptions/${audit_sub_id}/resourceGroups/${audit_resource_group}/providers/microsoft.insights/components/${audit_resource_name_app_insights}"
                            },
                            {
                                "name": "Version",
                                "value": "1.0"
                            }
                        ],
                        "type": "Extension/AppInsightsExtension/PartType/AspNetOverviewPinnedPart",
                        "asset": {
                            "idInputName": "id",
                            "type": "ApplicationInsights"
                        },
                        "defaultMenuItemId": "overview"
                    }
                },
                "1": {
                    "position": {
                        "x": 2,
                        "y": 0,
                        "colSpan": 1,
                        "rowSpan": 1
                    },
                    "metadata": {
                        "inputs": [
                            {
                                "name": "ComponentId",
                                "value": {
                                    "Name": "DEV-UKS-COHMAN-WEB",
                                    "SubscriptionId": "${audit_sub_id}",
                                    "ResourceGroup": "${audit_resource_group}"
                                }
                            },
                            {
                                "name": "Version",
                                "value": "1.0"
                            }
                        ],
                        "type": "Extension/AppInsightsExtension/PartType/ProactiveDetectionAsyncPart",
                        "asset": {
                            "idInputName": "ComponentId",
                            "type": "ApplicationInsights"
                        },
                        "defaultMenuItemId": "ProactiveDetection"
                    }
                }
            }
        }
    }
}