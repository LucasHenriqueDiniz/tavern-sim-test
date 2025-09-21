## Objetivo
<!-- Bugfix/feature/tooling/workflow -->

## Checklist
- [ ] Projeto abre no **Unity 2022.3.62f1** sem erros de compilação.
- [ ] Testes **EditMode/PlayMode** passam localmente (Runner ou `-runTests`).  
- [ ] Pacotes em `Packages/manifest.json` e `packages-lock.json` **consistentes**; não removi deps sem combinar.
- [ ] Se adicionei scripts ou assets, **.meta** correspondentes estão no commit.
- [ ] Não há **tipos duplicados** (mesmo namespace+nome) e `TavernSim.Agents.{Customer,Waiter}` continuam **MonoBehaviours**.
- [ ] Se mexi em navegação, confirmei **AI Navigation** instalado e NavMesh gerando em cena.
- [ ] Se mexi no bootstrap, mantive o propósito do **DevBootstrap** (graybox, registro de sistemas) e documentei mudanças.
- [ ] Atualizei o **Unity contributor handbook** se alterei práticas de workflow.

## Notas
<!-- riscos, limitações, follow-ups -->
