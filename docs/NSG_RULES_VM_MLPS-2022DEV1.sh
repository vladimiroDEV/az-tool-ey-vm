
#!/bin/bash

#az ad sp create-for-rbac --name "MLPS-SP-RBAC" --role Contributor --scopes /subscriptions/833d8542-cdc2-4468-bdb7-e34e3e0e71b5

AZURE_CLIENT_ID="134e1661-d6fb-4df7-8d53-8a49d3c11c6e"
AZURE_SECRET="PF88Q~zKbt1VS3jkXY41ZLawSTv7EKWcrjxYicUl"
AZURE_TENANT="adb53b4f-b05f-4dcb-a2e1-9111380568c3"
SUBSCRIPTIONID="833d8542-cdc2-4468-bdb7-e34e3e0e71b5"

VM="MLPS-WS2022DEV1"
RESOURCE_GROUP="MLPSDEV"
NSG_NAME="MLPS-WS2022DEV1-nsg"

# ========================================
# CONFIGURAZIONE REGOLE NSG
# Formato: "RULE_NAME|PRIORITY|PORT|DESCRIPTION"
# ========================================
NSG_RULES=(
  "AllowRDPInbound|234|3389|Allow RDP from multiple IPs"
  "AllowMSSQLInbound|110|1433|Allow MSSQL from multiple IPs"
  # Aggiungi altre regole qui se necessario:
  # "AllowSSHInbound|240|22|Allow SSH from multiple IPs"
  # "AllowHTTPSInbound|250|443|Allow HTTPS from multiple IPs"
)

# Ottieni l'indirizzo IP pubblico
PUBLIC_IP=$(curl -s ifconfig.me)

echo "=========================================="
echo "IP pubblico rilevato: $PUBLIC_IP"
echo "=========================================="

az login --service-principal --username $AZURE_CLIENT_ID --password $AZURE_SECRET --tenant $AZURE_TENANT
az account set --subscription $SUBSCRIPTIONID

# Avvio la VM SP DEV
#az vm start --resource-group $RESOURCE_GROUP --name $VM

# Verifica se l'indirizzo IP è già presente nella regola specifica
check_ip_in_rule() {
  local ip=$1
  local rule_name=$2
  local result
  result=$(az network nsg rule show --resource-group $RESOURCE_GROUP --nsg-name $NSG_NAME --name $rule_name --query "sourceAddressPrefixes" --output tsv 2>/dev/null | grep -w "$ip")
  if [ -n "$result" ]; then
    echo "Indirizzo IP $ip già presente nella regola $rule_name."
    return 0
  else
    echo "Indirizzo IP $ip non trovato nella regola $rule_name."
    return 1
  fi
}

# Ottieni gli IP esistenti dalla regola
get_existing_ips() {
  local rule_name=$1
  az network nsg rule show --resource-group $RESOURCE_GROUP --nsg-name $NSG_NAME --name $rule_name --query "sourceAddressPrefixes" --output tsv 2>/dev/null | tr '\n' ' '
}

# Funzione per aggiungere o aggiornare una regola NSG
update_or_create_rule() {
  local rule_name=$1
  local priority=$2
  local port=$3
  local description=$4
  
  echo ""
  echo "=========================================="
  echo "Gestione regola: $rule_name (porta $port)"
  echo "=========================================="
  
  # Verifica se la regola esiste
  EXISTING_RULE=$(az network nsg rule show --resource-group $RESOURCE_GROUP --nsg-name $NSG_NAME --name $rule_name 2>/dev/null)
  
  if [ -n "$EXISTING_RULE" ]; then
    # La regola esiste, verifica se l'IP è già presente
    if check_ip_in_rule $PUBLIC_IP $rule_name; then
      echo "L'indirizzo IP $PUBLIC_IP è già presente nella regola $rule_name."
      return 0
    fi
    
    # Ottieni gli IP esistenti
    EXISTING_IPS=$(get_existing_ips $rule_name)
    echo "IP esistenti nella regola: $EXISTING_IPS"
    
    # Aggiungi il nuovo IP alla lista
    NEW_IPS="$EXISTING_IPS $PUBLIC_IP"
    
    echo "Aggiornamento della regola $rule_name aggiungendo l'IP $PUBLIC_IP..."
    az network nsg rule update \
      --resource-group $RESOURCE_GROUP \
      --nsg-name $NSG_NAME \
      --name $rule_name \
      --source-address-prefixes $NEW_IPS
    
    echo "✓ IP $PUBLIC_IP aggiunto con successo alla regola $rule_name."
  else
    # La regola non esiste, creala
    echo "La regola $rule_name non esiste. Creazione in corso..."
    az network nsg rule create \
      --resource-group $RESOURCE_GROUP \
      --nsg-name $NSG_NAME \
      --name $rule_name \
      --priority $priority \
      --direction Inbound \
      --access Allow \
      --protocol Tcp \
      --source-address-prefixes $PUBLIC_IP \
      --source-port-ranges '*' \
      --destination-address-prefixes '*' \
      --destination-port-ranges $port \
      --description "$description"
    
    echo "✓ Regola $rule_name creata con successo con IP $PUBLIC_IP."
  fi
}

# Cicla attraverso tutte le regole configurate
for rule_config in "${NSG_RULES[@]}"; do
  # Estrai i parametri dalla configurazione
  IFS='|' read -r rule_name priority port description <<< "$rule_config"
  
  # Aggiorna/crea la regola
  update_or_create_rule "$rule_name" "$priority" "$port" "$description"
done

echo ""
echo "=========================================="
echo "✓ Processo completato!"
echo "=========================================="
