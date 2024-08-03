<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
                xmlns="http://www.idpf.org/2007/opf" xmlns:dc="http://purl.org/dc/elements/1.1/">
    <xsl:output method="xml" indent="yes"/>
    <xsl:template match="/">
        <package  version="3.0" unique-identifier="uid">
            <metadata>
                <dc:identifier id="uid">code.google.com.epub-samples.epub30-spec</dc:identifier>
                <dc:title>Сборник статей Александра Зубченко</dc:title>
                <dc:language>ru</dc:language>
                <meta property="dcterms:created">2024-08-02T16:38:35Z</meta>
            </metadata>
            <manifest>
                <item id="ttl" href="xhtml/titlePage.xhtml" media-type="application/xhtml+xml"/>
                <item id="nav" href="xhtml/nav.xhtml" media-type="application/xhtml+xml" properties="nav"/>
                <item id="css" href="css/style.css" media-type="text/css"/>
                <xsl:apply-templates select="/Objects/Object/Property" mode="item"/>
                <xsl:apply-templates select="/Objects/Object/Property[Property[@Name='ImageName']!='']" mode="img"/>
            </manifest>
            <spine>
                <itemref idref="ttl"/>
                <itemref idref="nav" linear="no"/>
                <xsl:apply-templates select="/Objects/Object/Property" mode="itemref"/>
            </spine>
        </package>        
    </xsl:template>
    
    <xsl:template match="Property" mode="item">
        <item>
            <xsl:attribute name="id">
                <xsl:value-of select="concat('item_',Property[@Name='Id'])" />
            </xsl:attribute>        
            <xsl:attribute name="href">
                <xsl:value-of select="concat('xhtml/',Property[@Name='FileName'])" />
            </xsl:attribute>
            <xsl:attribute name="media-type">application/xhtml+xml</xsl:attribute>
        </item>
    </xsl:template>
    
    <xsl:template match="Property" mode="img">
        <item>
            <xsl:attribute name="id">
                <xsl:value-of select="concat('img_',Property[@Name='Id'])" />
            </xsl:attribute>        
            <xsl:attribute name="href">
                <xsl:value-of select="concat('img/',Property[@Name='ImageName'])" />
            </xsl:attribute>
            <xsl:attribute name="media-type">image/jpeg</xsl:attribute>
        </item>
    </xsl:template>    
    
    <xsl:template match="Property" mode="itemref">
        <itemref>
            <xsl:attribute name="idref">
                <xsl:value-of select="concat('item_',Property[@Name='Id'])" />
            </xsl:attribute>        
        </itemref>
    </xsl:template>     
</xsl:stylesheet>