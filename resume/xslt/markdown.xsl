<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="text" indent="no" encoding="UTF-8"/>

  <xsl:template match="/resume">
    <xsl:text># </xsl:text><xsl:value-of select="name"/>
    <xsl:text>&#10;</xsl:text><xsl:value-of select="headline"/>
    <xsl:text>&#10;&#10;</xsl:text>

    <xsl:text>## Contact&#10;</xsl:text>
    <xsl:text>- 📧 </xsl:text>[<xsl:value-of select="email"/>](mailto:<xsl:value-of select="email"/>)
    <xsl:text>&#10;- 📞 </xsl:text>[<xsl:value-of select="phone"/>](tel:<xsl:value-of select="phone"/>)
    <xsl:text>&#10;- 📍 </xsl:text><xsl:value-of select="location"/>
    <xsl:for-each select="links/link">
      <xsl:text>&#10;- 🌐 </xsl:text>
      <xsl:choose>
          <xsl:when test="contains(., 'linkedin')">[LinkedIn](<xsl:value-of select="."/>)</xsl:when>
          <xsl:when test="contains(., 'github')">[GitHub](<xsl:value-of select="."/>)</xsl:when>
          <xsl:otherwise>[<xsl:value-of select="."/>](<xsl:value-of select="."/>)</xsl:otherwise>
      </xsl:choose>
    </xsl:for-each>
    <xsl:text>&#10;&#10;</xsl:text>

    <xsl:text>## Summary&#10;</xsl:text><xsl:value-of select="summary"/>
    <xsl:text>&#10;&#10;</xsl:text>

    <xsl:text>## Work Experience&#10;</xsl:text>
    <xsl:for-each select="experiences/experience">
      <xsl:text>### </xsl:text><xsl:value-of select="position"/><xsl:text>  </xsl:text>
      <xsl:text>&#10;#### </xsl:text>            
      <xsl:choose>
          <xsl:when test="website != ''">[<xsl:value-of select="company"/>](<xsl:value-of select="website"/>)</xsl:when>
          <xsl:otherwise><xsl:value-of select="company"/></xsl:otherwise>
      </xsl:choose>
      <xsl:text>  </xsl:text>      
      <xsl:text> &#40;</xsl:text><xsl:value-of select="period"/>
      <xsl:text>&#41;&#10;</xsl:text>
      <xsl:value-of select="description" disable-output-escaping="yes"/>
      <xsl:text>&#10;&#10;</xsl:text>
    </xsl:for-each>

    <xsl:text>## Skills&#10;</xsl:text>
    <xsl:for-each select="skills/skill">
      <xsl:text>- **</xsl:text><xsl:value-of select="name"/>
      <xsl:text>:** </xsl:text>
      <xsl:for-each select="keywords/keyword">
        <xsl:value-of select="."/>
        <xsl:if test="position() != last()">, </xsl:if>
      </xsl:for-each>
      <xsl:text>&#10;</xsl:text>
    </xsl:for-each>

    <xsl:text>&#10;## Certifications&#10;</xsl:text>
    <xsl:for-each select="certifications/certification">
      <xsl:text>&#10;- **</xsl:text><xsl:value-of select="title"/><xsl:text>**  &#10;</xsl:text>
      <xsl:value-of select="issuer"/>
      <xsl:text> &#40;</xsl:text><xsl:value-of select="date"/><xsl:text>&#41;&#10;</xsl:text>
      <xsl:if test="website != ''">[<xsl:value-of select="label"/>](<xsl:value-of select="website"/>)</xsl:if>       
    </xsl:for-each>

    <xsl:text>&#10;## Education&#10;</xsl:text>
    <xsl:for-each select="educations/education">
      <xsl:text>- **</xsl:text><xsl:value-of select="degree"/>
      <xsl:text>** in </xsl:text><xsl:value-of select="area"/><xsl:text>  </xsl:text>
      <xsl:if test="website != ''">
        [<xsl:value-of select="school"/>](<xsl:value-of select="website"/>)
      </xsl:if>       
      <xsl:text> &#40;</xsl:text><xsl:value-of select="period"/>
      <xsl:text>&#41;&#10;</xsl:text>
    </xsl:for-each>

    <xsl:text>&#10;## Languages&#10;</xsl:text>
    <xsl:for-each select="languages/language">
      <xsl:text>- </xsl:text><xsl:value-of select="name"/>
      <xsl:text>: </xsl:text>
      <xsl:choose>
        <xsl:when test="fluency != ''"><xsl:value-of select="fluency"/></xsl:when>
        <xsl:otherwise>Level <xsl:value-of select="level"/>/5</xsl:otherwise>
      </xsl:choose>
      <xsl:text>&#10;</xsl:text>
    </xsl:for-each>
        
  </xsl:template>
</xsl:stylesheet>