# Te'Rha Patcher
Te'Rha Patcher è un'applicazione progettata per semplificare la distribuzione degli aggiornamenti di un'applicazione o di un gioco. Questa applicazione consente agli sviluppatori di creare file patch contenenti solo le differenze tra la vecchia e la nuova versione dell'applicazione, riducendo così le dimensioni complessive dei file da distribuire.

## Funzionalità
- Trova le differenze con VCDIFF: Te'Rha Patcher utilizza l'algoritmo VCDIFF per generare file patch che contengono solo le differenze tra i file della vecchia e della nuova versione dell'applicazione. Questo metodo consente di ridurre notevolmente le dimensioni dei file patch, migliorando l'efficienza della distribuzione degli aggiornamenti.

- Controllo per byte per qualsiasi tipo di file: Te'Rha Patcher implementa un controllo per byte che consente di gestire qualsiasi tipo di file, inclusi file binari e di testo. Ciò significa che Te'Rha Patcher è in grado di generare file patch per una vasta gamma di file, assicurando che tutte le modifiche vengano correttamente catturate e applicate durante il processo di aggiornamento.

- Crea file patch per tutti i tipi di file: Grazie al controllo per byte, Te'Rha Patcher può generare file patch per qualsiasi tipo di file, indipendentemente dal loro formato o contenuto. Questo rende l'applicazione estremamente flessibile e adatta a una varietà di scenari di distribuzione degli aggiornamenti.

- Uploader di file patch su GitHub: Te'Rha Patcher include un uploader di file patch che consente agli sviluppatori di caricare i file patch generati su GitHub. Questo semplifica notevolmente il processo di distribuzione degli aggiornamenti, consentendo agli utenti di scaricare facilmente i file patch e applicarli alla propria installazione dell'applicazione.

## Dipendenze del progetto
Te'Rha Patcher utilizza le seguenti dipendenze:

- Autofac: Un framework di dependency injection per la gestione delle dipendenze nel codice .NET.
- ICSharpCode.SharpZipLib: Una libreria per la compressione e decompressione dei file in formato ZIP.
- VCDIFF: Una libreria per generare e applicare file patch utilizzando il formato VCDIFF.
- Octokit: Una libreria per interagire con l'API di GitHub.
## Contributi
Se desideri contribuire a Te'Rha Patcher, sei il benvenuto! Puoi contribuire segnalando bug, proponendo nuove funzionalità o inviando pull request al repository GitHub del progetto.

## Licenza
Te'Rha Patcher è rilasciato sotto la licenza MIT. Vedi il file LICENSE per ulteriori informazioni.
