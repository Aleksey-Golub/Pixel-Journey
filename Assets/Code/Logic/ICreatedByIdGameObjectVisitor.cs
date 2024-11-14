﻿
public interface ICreatedByIdGameObjectVisitor
{
    void Visit(SellBoard sellBoard);
    void Visit(UpgradeBoard upgradeBoard);
    void Visit(ResourceSource resourceSource);
    void Visit(ResourceStorage resourceStorage);
    void Visit(Converter converter);
    void Visit(Workbench workbench);
    void Visit(Workshop workshop);
}