provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "iot_rg" {
  name     = "rg-smart-factory"
  location = "East US"
}

resource "azurerm_iothub" "iot_hub" {
  name                = "iothub-smart-factory"
  resource_group_name = azurerm_resource_group.iot_rg.name
  location            = azurerm_resource_group.iot_rg.location
  sku {
    name     = "F1" # Free tier
    capacity = 1
  }
}

resource "azurerm_key_vault" "kv" {
  name                = "kv-smart-factory"
  location            = azurerm_resource_group.iot_rg.location
  resource_group_name = azurerm_resource_group.iot_rg.name
  tenant_id           = "YOUR_TENANT_ID"
  sku_name            = "standard"
}

resource "azurerm_signalr_service" "signalr" {
  name                = "signalr-smart-factory"
  location            = azurerm_resource_group.iot_rg.location
  resource_group_name = azurerm_resource_group.iot_rg.name
  sku {
    name     = "Free_F1"
    capacity = 1
  }
}

# Logic App for Notifications
resource "azurerm_logic_app_workflow" "notification_workflow" {
  name                = "logic-smart-factory-notifications"
  location            = azurerm_resource_group.iot_rg.location
  resource_group_name = azurerm_resource_group.iot_rg.name
}

# Managed Identity for AKS to access Key Vault
resource "azurerm_user_assigned_identity" "aks_identity" {
  name                = "id-smart-factory-aks"
  location            = azurerm_resource_group.iot_rg.location
  resource_group_name = azurerm_resource_group.iot_rg.name
}

# Azure Service Bus for Event Bus
resource "azurerm_servicebus_namespace" "sb_namespace" {
  name                = "sb-smart-factory"
  location            = azurerm_resource_group.iot_rg.location
  resource_group_name = azurerm_resource_group.iot_rg.name
  sku                 = "Basic" # Free tier/Basic for development
}

resource "azurerm_servicebus_topic" "event_bus_topic" {
  name         = "smart_factory_event_bus"
  namespace_id = azurerm_servicebus_namespace.sb_namespace.id
}
