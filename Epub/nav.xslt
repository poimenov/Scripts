<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="2.0"
                xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
    <xsl:output method="xml" indent="yes"/>
    <xsl:key name="year" match="Property[@Name='Year']" use="text()" />
    <xsl:key name="month" match="Property[@Name='Month']" use="text()" />
    <xsl:template match="/">
        <html lang="ru">
            <head>
                <meta charset="utf-8" />
                <title>Содержание</title>
                <link rel="stylesheet" type="text/css" href="../css/style.css" />
            </head>
            <body>
                <nav epub:type="toc" id="toc">
                    <h1 class="title">Содержание</h1> 
                    <ul>
                        <li id="ttl"><a href="titlePage.xhtml">Титульная страница</a></li>
                        <li id="nav"><a href="nav.xhtml">Содержание</a></li>                    
                        <xsl:for-each select="/Objects/Object/Property/Property[generate-id() = generate-id(key('year',text())[1])]">                            
                            <xsl:sort select="text()" />
                            <xsl:variable name="currentYear" select="text()"/>
                            <li><xsl:value-of select="$currentYear"/> год
                                <ul>
                                    <xsl:for-each select="/Objects/Object/Property/Property[generate-id() = generate-id(key('month',text())[1])]">
                                        <xsl:sort select="text()" />
                                        <xsl:variable name="currentMonth" select="text()"/>
                                        <xsl:if test="/Objects/Object/Property[Property[@Name='Year']=$currentYear and Property[@Name='Month']=$currentMonth]">
                                            <li><xsl:apply-templates select="." mode="month" />
                                                <ul>
                                                    <xsl:apply-templates select="/Objects/Object/Property[Property[@Name='Year']=$currentYear and Property[@Name='Month']=$currentMonth]" mode="item">
                                                        <xsl:sort select="Property[@Name='DateTime']" />
                                                    </xsl:apply-templates>
                                                </ul>
                                            </li>
                                        </xsl:if>           
                                    </xsl:for-each>
                                </ul>
                            </li>
                        </xsl:for-each>                    
                    </ul>                   
                </nav>
            </body>
        </html>  
    </xsl:template>
    
    <xsl:template match="Property" mode="item">
        <li>
            <xsl:attribute name="id">
                <xsl:value-of select="concat('item_',Property[@Name='Id'])" />
            </xsl:attribute>
            <a>
                <xsl:attribute name="href">
                    <xsl:value-of select="Property[@Name='FileName']" />
                </xsl:attribute>
                <xsl:value-of select="Property[@Name='Title']"/>
            </a>
        </li>
    </xsl:template>
    
    <xsl:template match="*" mode="month">
        <xsl:choose>
            <xsl:when test="text()='01'">Январь</xsl:when>
            <xsl:when test="text()='02'">Февраль</xsl:when>
            <xsl:when test="text()='03'">Март</xsl:when>
            <xsl:when test="text()='04'">Апрель</xsl:when>
            <xsl:when test="text()='05'">Май</xsl:when>
            <xsl:when test="text()='06'">Июнь</xsl:when>
            <xsl:when test="text()='07'">Июль</xsl:when>
            <xsl:when test="text()='08'">Август</xsl:when>
            <xsl:when test="text()='09'">Сентябрь</xsl:when>
            <xsl:when test="text()='10'">Октябрь</xsl:when>
            <xsl:when test="text()='11'">Ноябрь</xsl:when>
            <xsl:when test="text()='12'">Декабрь</xsl:when>
        </xsl:choose>
    </xsl:template>
</xsl:stylesheet>