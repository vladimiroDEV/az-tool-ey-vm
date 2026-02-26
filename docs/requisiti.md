Mi serve un tool  con interfaccia web. in dotne core 8, pagine razor per applicare le regole su azure utilizzando il service principal. 

in pratica vedi quello che faccio sallo script .sh , lo vorri fare da interfaccia web 

devo poter lavorare su più tenant, 
i tenant sono configurati in appsettings: i blocchi : tenant subscription, client id e client secret

dall'interfaccia web devo poter selezionare il tenant/subscrption  che mi serve e su cui lavoroare.

operazione che devo peter fare : 
selezionare il resource group (qualora user prinsipal e è bilitato su più di uno )
accendere e spegnere le vm presente in quel resourse group
aggiungere nsd alla VM in quel resourse group. ( in pratica devo inserire manualemnte in. e selezionare il protocollo da abilitare , per ora RDP e MS SQL)

interfaccia web deve essere protetta da user e password (configurati sempre in appsettings )

implementa questa piccola applicazione, se hai domande cheidi dopo l'analisi dei requisito (preparando gi dei suggeriemti validi)
