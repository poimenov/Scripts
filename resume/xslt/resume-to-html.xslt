<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:output method="html" encoding="UTF-8"/>
    <xsl:template match="/">
        <html>
        <head><title>Resume</title></head>
        <body>
            <h1><xsl:value-of select="/resume/name"/></h1>
            <h2><xsl:value-of select="/resume/headline"/></h2>
            <p><b>Email:</b> <xsl:value-of select="/resume/email"/></p>
            <p><b>Phone:</b> <xsl:value-of select="/resume/phone"/></p>
            <p><b>Location:</b> <xsl:value-of select="/resume/location"/></p>
            <xsl:if test="/resume/picturePath and string-length(/resume/picturePath)&gt;0">
                <img src="{/resume/picturePath}" alt="Picture" style="max-width:150px;"/>
            </xsl:if>
            <h3>Links</h3>
            <ul>
                <xsl:for-each select="/resume/links/link">
                    <li><a href="{.}"><xsl:value-of select="."/></a></li>
                </xsl:for-each>
            </ul>
            <h3>Summary</h3>
            <div><xsl:value-of select="/resume/summary" disable-output-escaping="yes"/></div>
        </body>
        </html>
    </xsl:template>
</xsl:stylesheet>